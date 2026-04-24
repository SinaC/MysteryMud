using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.GameData.Definitions;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Helpers;

public static class CombatHelpers
{
    public static void EnterCombat(World world, GameState state, EntityId source, EntityId target)
    {
        if (!CharacterHelpers.IsAlive(world, source, target)) return;

        // flag both as in combat with each other, with the target striking back after a delay
        if (!world.Has<CombatState>(source))
        {
            world.Add(source, new CombatState { Target = target, RoundDelay = 0 });
            if (!world.Has<NewCombatantTag>(source))
                world.Add<NewCombatantTag>(source);
        }

        if (!world.Has<CombatState>(target))
        {
            world.Add(target, new CombatState { Target = source, RoundDelay = 1 });
            if (!world.Has<NewCombatantTag>(target))
                world.Add<NewCombatantTag>(target);
        }

        AddCombatClaim(world, state, target, source);
    }

    public static void RemoveFromCombat(World world, GameState state, EntityId character)
    {
        if (world.Has<CombatState>(character))
            world.Remove<CombatState>(character);
        if (world.Has<NewCombatantTag>(character))
            world.Remove<NewCombatantTag>(character);
        if (world.Has<CombatInitiator>(character))
            world.Remove<CombatInitiator>(character);
        if (world.Has<ActiveThreatTag>(character))
            world.Remove<ActiveThreatTag>(character);
        ref var threatTable = ref world.TryGetRef<ThreatTable>(character, out var hasThreatTable);
        if (hasThreatTable)
        {
            threatTable.Threat.Clear();
            threatTable.LastUpdateTick = state.CurrentTick;
        }
    }

    private readonly static QueryDescription _inCombatQueryDesc = new QueryDescription()
        .WithAll<CombatState>();

    // TODO: this could be optimized by having a "Targeting" component that lists all entities targeting a given entity, so we don't have to scan everyone in the world for combat state every time someone dies. We would need to maintain this list as combat states are added/removed, but it would make removing combat state on death much more efficient.
    // mutually remove combat state from victim and anyone targeting the victim in one query if possible
    public static void RemoveFromAllCombat(World world, GameState state, EntityId character)
    {
        // clean up character's own state fully
        RemoveFromCombat(world, state, character);

        // for anyone targeting this character:
        // only remove their CombatState, do NOT call RemoveFromCombat
        // which would wrongly wipe their entire threat table
        world.Query(in _inCombatQueryDesc, (EntityId entity,
            ref CombatState combat) =>
        {
            if (combat.Target == character)
            {
                if (world.Has<CombatState>(entity))
                    world.Remove<CombatState>(entity);
                if (world.Has<NewCombatantTag>(entity))
                    world.Remove<NewCombatantTag>(entity);
                if (world.Has<CombatInitiator>(entity)) // they were initiator on someone else, irrelevant now
                    world.Remove<CombatInitiator>(entity);
                if (world.Has<ActiveThreatTag>(entity)) // no more active threat if not in combat
                    world.Remove<ActiveThreatTag>(entity);
            }
        });
    }

    private readonly static QueryDescription _threatQueryDesc = new QueryDescription()
        .WithAll<ThreatTable, ActiveThreatTag>();

    public static void RemoveFromAllThreatTable(World world, EntityId character) // TODO: optimize, this will loop on every NPC
    {
        world.Query(_threatQueryDesc, (EntityId _,
            ref ThreatTable threatTable,
            ref ActiveThreatTag _) =>
        {
            threatTable.Threat.Remove(character);
        });
    }

    public static bool TryDetermineLootOwner(World world, EntityId victim, EntityId killer, out EntityId looter)
    {
        if (killer == victim)
        {
            looter = EntityId.Invalid;
            return false;
        }

        if (world.Has<CombatInitiator>(victim))
        {
            ref var initiator = ref world.Get<CombatInitiator>(victim);
            var activeClaim = initiator.Claims
                .Where(c => !c.Forfeited && CharacterHelpers.IsAlive(world, c.Claimant))
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
        if (killer != EntityId.Invalid && world.Has<PlayerTag>(killer))
        {
            looter = killer;
            return true;
        }

        looter = EntityId.Invalid; // NPC killer or no killer — no loot intent
        return false;
    }

    private static readonly QueryDescription _initiatorQueryDesc = new QueryDescription()
        .WithAll<CombatInitiator>();

    public static void ForfeitAllClaims(World world, EntityId claimant)
    {
        world.Query(in _initiatorQueryDesc, (EntityId _,
            ref CombatInitiator initiator) =>
        {
            for (int i = 0; i < initiator.Claims.Count; i++)
            {
                if (initiator.Claims[i].Claimant == claimant)
                    initiator.Claims[i] = initiator.Claims[i] with { Forfeited = true };
            }
        });
    }

    public static void ForfeitClaim(World world, EntityId npc, EntityId claimant)
    {
        if (!world.Has<CombatInitiator>(npc)) return;

        ref var initiator = ref world.Get<CombatInitiator>(npc);
        var idx = initiator.Claims.FindIndex(c => c.Claimant == claimant);
        if (idx >= 0)
            initiator.Claims[idx] = initiator.Claims[idx] with { Forfeited = true };
    }

    public static void AddCombatClaim(World world, GameState state, EntityId npc, EntityId claimant)
    {
        if (!world.Has<NpcTag>(npc)) return; // only track on NPCs
        if (!world.Has<PlayerTag>(claimant)) return; // only players can claim

        if (!world.Has<CombatInitiator>(npc))
            world.Add(npc, new CombatInitiator { Claims = [] });

        ref var initiator = ref world.Get<CombatInitiator>(npc);

        // don't add duplicate claims
        if (initiator.Claims.Any(c => c.Claimant == claimant)) return;

        initiator.Claims.Add(new CombatClaim
        {
            Claimant = claimant,
            ClaimantGroup = world.Has<GroupMember>(claimant)
                ? world.Get<GroupMember>(claimant).Group
                : EntityId.Invalid,
            JoinedAtTick = state.CurrentTick,
            Forfeited = false
        });
    }
}
