using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Domain.Helpers;

public static class CharacterHelpers
{
    public static bool IsAlive(params Entity[] entities)
    {
        return entities.All(x => x.IsAlive() && !x.Has<Dead>());
    }

    public static bool SameRoom(Entity character1, Entity character2)
        => character1.Get<Location>().Room == character2.Get<Location>().Room;

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
        if (!IsAlive(source, target)) return;

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
