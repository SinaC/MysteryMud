using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Domain.Helpers;

public static class CombatHelpers
{
    public static void RemoveFromCombat(GameState state, Entity character)
    {
        character.Remove<CombatState>();
        character.Remove<NewCombatantTag>();
        character.Remove<CombatInitiator>();
        character.Remove<ActiveThreatTag>();
        ref var threatTable = ref character.TryGetRef<ThreatTable>(out var hasThreatTable);
        if (hasThreatTable)
        {
            threatTable.Threat.Clear();
            threatTable.LastUpdateTick = state.CurrentTick;
        }
    }

    // TODO: this could be optimized by having a "Targeting" component that lists all entities targeting a given entity, so we don't have to scan everyone in the world for combat state every time someone dies. We would need to maintain this list as combat states are added/removed, but it would make removing combat state on death much more efficient.
    // mutually remove combat state from victim and anyone targeting the victim in one query if possible
    public static void RemoveFromAllCombat(GameState state, Entity character)
    {
        // remove from combat
        character.Remove<CombatState>();
        // remove combat state for anyone targeting this entity
        var query = new QueryDescription()
          .WithAll<CombatState>();
        state.World.Query(query, (Entity actor, ref CombatState combat) =>
        {
            if (combat.Target == character)
                actor.Remove<CombatState>();
        });
    }

    public static void RemoveFromAllThreatTable(World world, Entity character) // TODO: optimize, this will loop on every NPC
    {
        var query = new QueryDescription()
            .WithAll<ThreatTable, ActiveThreatTag>();
        world.Query(query, (Entity actor, ref ThreatTable threatTable) =>
        {
            threatTable.Threat.Remove(character);
        });
    }


    public static Entity DetermineLootOwner(Entity victim, Entity killer)
    {
        if (victim.Has<CombatInitiator>())
        {
            ref var initiator = ref victim.Get<CombatInitiator>();
            var activeClaim = initiator.Claims
                .Where(c => !c.Forfeited && CharacterHelpers.IsAlive(c.Claimant))
                .OrderBy(c => c.JoinedAtTick)
                .FirstOrDefault();

            if (activeClaim.Claimant != Entity.Null)
                return activeClaim.Claimant;
        }
        return killer; // no valid claims, killer gets it
    }

    public static void EnterCombat(GameState state, Entity source, Entity target)
    {
        if (!CharacterHelpers.IsAlive(source, target)) return;

        if (!source.Has<CombatState>())
        {
            source.Add(new CombatState { Target = target, RoundDelay = 0 });
            source.Add<NewCombatantTag>();
        }

        if (!target.Has<CombatState>())
        {
            target.Add(new CombatState { Target = source, RoundDelay = 1 });
            target.Add<NewCombatantTag>();
        }

        AddCombatClaim(target, source, state.CurrentTick);
    }

    public static void ForfeitClaim(Entity npc, Entity claimant)
    {
        if (!npc.Has<CombatInitiator>()) return;

        ref var initiator = ref npc.Get<CombatInitiator>();
        var idx = initiator.Claims.FindIndex(c => c.Claimant == claimant);
        if (idx >= 0)
            initiator.Claims[idx] = initiator.Claims[idx] with { Forfeited = true };
    }

    public static void AddCombatClaim(Entity npc, Entity claimant, long currentTick)
    {
        if (!npc.Has<NpcTag>()) return; // only track on NPCs
        if (!claimant.Has<PlayerTag>()) return; // only players can claim

        if (!npc.Has<CombatInitiator>())
            npc.Add(new CombatInitiator { Claims = [] });

        ref var initiator = ref npc.Get<CombatInitiator>();

        // don't add duplicate claims
        if (initiator.Claims.Any(c => c.Claimant == claimant)) return;

        initiator.Claims.Add(new CombatClaim
        {
            Claimant = claimant,
            ClaimantGroup = claimant.Has<GroupMember>()
                ? claimant.Get<GroupMember>().Group
                : Entity.Null,
            JoinedAtTick = currentTick,
            Forfeited = false
        });
    }
}
