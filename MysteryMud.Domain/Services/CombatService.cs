using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Domain.Services;

public class CombatService : ICombatService
{
    private readonly EntitySet _inCombatEntitySet;
    private readonly EntitySet _activeThreatEntitySet;
    private readonly EntitySet _combatInitiatorEntitySet;

    public CombatService(World world)
    {
        _inCombatEntitySet = world
            .GetEntities()
            .With<CombatState>()
            .AsSet();

        _activeThreatEntitySet = world
            .GetEntities()
            .With<ThreatTable>()
            .With<ActiveThreatTag>()
            .AsSet();

        _combatInitiatorEntitySet = world
            .GetEntities()
            .With<CombatInitiator>()
            .AsSet();
    }

    public void EnterCombat(GameState state, Entity source, Entity target)
    {
        if (!CharacterHelpers.IsAlive(source, target)) return;

        // flag both as in combat with each other, with the target striking back after a delay
        if (!source.Has<CombatState>())
        {
            source.Set(new CombatState { Target = target, RoundDelay = 0 });
            if (!source.Has<NewCombatantTag>())
                source.Set<NewCombatantTag>();
        }

        if (!target.Has<CombatState>())
        {
            target.Set(new CombatState { Target = source, RoundDelay = 1 });
            if (!target.Has<NewCombatantTag>())
                target.Set<NewCombatantTag>();
        }

        AddCombatClaim(target, source, state.CurrentTick);
    }

    public void RemoveFromCombat(GameState state, Entity character)
    {
        if (character.Has<CombatState>())
            character.Remove<CombatState>();
        if (character.Has<NewCombatantTag>())
            character.Remove<NewCombatantTag>();
        if (character.Has<CombatInitiator>())
            character.Remove<CombatInitiator>();
        if (character.Has<ActiveThreatTag>())
            character.Remove<ActiveThreatTag>();
        if (character.Has<ThreatTable>())
        {
            ref var threatTable = ref character.Get<ThreatTable>();
            threatTable.Entries.Clear();
            threatTable.LastUpdateTick = state.CurrentTick;
        }
    }

    // TODO: this could be optimized by having a "Targeting" component that lists all entities targeting a given entity, so we don't have to scan everyone in the world for combat state every time someone dies. We would need to maintain this list as combat states are added/removed, but it would make removing combat state on death much more efficient.
    // mutually remove combat state from victim and anyone targeting the victim in one query if possible
    public void RemoveFromAllCombat(GameState state, Entity character)
    {
        // clean up character's own state fully
        RemoveFromCombat(state, character);

        // for anyone targeting this character:
        // only remove their CombatState, do NOT call RemoveFromCombat
        // which would wrongly wipe their entire threat table
        foreach(var entity in _inCombatEntitySet.GetEntities())
        {
            ref var combat = ref entity.Get<CombatState>();
            if (combat.Target == character)
            {
                if (entity.Has<CombatState>())
                    entity.Remove<CombatState>();
                if (entity.Has<NewCombatantTag>())
                    entity.Remove<NewCombatantTag>();
                if (entity.Has<CombatInitiator>()) // they were initiator on someone else, irrelevant now
                    entity.Remove<CombatInitiator>();
                if (entity.Has<ActiveThreatTag>()) // no more active threat if not in combat
                    entity.Remove<ActiveThreatTag>();
            }
        }
    }

    public void RemoveFromAllThreatTable(World world, Entity character) // TODO: optimize, this will loop on every NPC
    {
        foreach(var entity in _activeThreatEntitySet.GetEntities())
        {
            ref var threatTable = ref entity.Get<ThreatTable>();
            threatTable.Entries.Remove(character);
        }
    }

    public bool TryDetermineLootOwner(Entity victim, Entity killer, out Entity looter)
    {
        if (victim.Has<CombatInitiator>())
        {
            ref var initiator = ref victim.Get<CombatInitiator>();
            var activeClaim = initiator.Claims
                .Where(c => !c.Forfeited && CharacterHelpers.IsAlive(c.Claimant))
                .OrderBy(c => c.JoinedAtTick)
                .Cast<CombatClaim?>()  // nullable so FirstOrDefault returns null, not zeroed struct
                .FirstOrDefault();

            if (activeClaim.HasValue)
            {
                looter = activeClaim.Value.Claimant;
                return true;
            }
        }

        // killer could be an NPC (e.g. orc kills a player), Entity.Null, or a player
        if (killer != default && killer.Has<PlayerTag>())
        {
            looter = killer;
            return true;
        }

        looter = default; // NPC killer or no killer — no loot intent
        return false;
    }

    public void ForfeitAllClaims(World world, Entity claimant)
    {
        foreach(var entity in _combatInitiatorEntitySet.GetEntities())
        {
            ref var initiator = ref entity.Get<CombatInitiator>();
            for (int i = 0; i < initiator.Claims.Count; i++)
            {
                if (initiator.Claims[i].Claimant == claimant)
                    initiator.Claims[i] = initiator.Claims[i] with { Forfeited = true };
            }
        }
    }

    public void ForfeitClaim(Entity npc, Entity claimant)
    {
        if (!npc.Has<CombatInitiator>()) return;

        ref var initiator = ref npc.Get<CombatInitiator>();
        var idx = initiator.Claims.FindIndex(c => c.Claimant == claimant);
        if (idx >= 0)
            initiator.Claims[idx] = initiator.Claims[idx] with { Forfeited = true };
    }

    public void AddCombatClaim(Entity npc, Entity claimant, long currentTick)
    {
        if (!npc.Has<NpcTag>()) return; // only track on NPCs
        if (!claimant.Has<PlayerTag>()) return; // only players can claim

        if (!npc.Has<CombatInitiator>())
            npc.Set(new CombatInitiator { Claims = [] });

        ref var initiator = ref npc.Get<CombatInitiator>();

        // don't add duplicate claims
        if (initiator.Claims.Any(c => c.Claimant == claimant)) return;

        initiator.Claims.Add(new CombatClaim
        {
            Claimant = claimant,
            ClaimantGroup = claimant.Has<GroupMember>()
                ? claimant.Get<GroupMember>().Group
                : default,
            JoinedAtTick = currentTick,
            Forfeited = false
        });
    }
}
