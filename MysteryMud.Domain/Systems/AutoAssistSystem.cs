using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class AutoAssistSystem
{
    private readonly IEventBuffer<RoomEnteredEvent> _roomEnteredEvent;

    public AutoAssistSystem(IEventBuffer<RoomEnteredEvent> roomEnteredEvent)
    {
        _roomEnteredEvent = roomEnteredEvent;
    }

    public void TickCombatInitiated(GameState state)  // pass 1 and 3: tag-driven only
    {
        ProcessNewCombatants(state);
    }

    public void TickMovement(GameState state)          // pass 2: tag-driven + room entry
    {
        ProcessNewCombatants(state);
        ProcessRoomEntries(state);   // consumes and clears RoomEnteredEvents
    }

    private void ProcessNewCombatants(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<CombatState, NewCombatantTag>();
        state.World.Query(query, (Entity entity, ref CombatState combat, ref NewCombatantTag tag) =>
        {
            entity.Remove<NewCombatantTag>(); // consume immediately
            PropagateAssist(state, entity, combat.Target);
        });

    }

    private void ProcessRoomEntries(GameState state)
    {
        foreach (ref var evt in _roomEnteredEvent.GetAll())
            CheckAssistOnRoomEntry(state, ref evt);
    }

    private void CheckAssistOnRoomEntry(GameState state, ref RoomEnteredEvent evt)
    {
        ref var roomContents = ref evt.ToRoom.Get<RoomContents>();

        foreach (var character in roomContents.Characters)
        {
            if (character == evt.Entity) continue;
            if (!character.Has<CombatState>()) continue;

            ref var combat = ref character.Get<CombatState>();

            // check if entering entity should assist this combatant
            if (ShouldAssist(evt.Entity, character, combat.Target, out var reason))
            {
                TryAssist(evt.Entity, combat.Target, reason);
                // also check charmies of the entering entity
                if (evt.Entity.Has<Charmies>())
                {
                    foreach (var charmie in evt.Entity.Get<Charmies>().Entities)
                        TryAssist(charmie, combat.Target, AssistReason.Charmed);
                }
                return; // assist first eligible combatant only
            }
        }
    }

    private bool ShouldAssist(Entity candidate, Entity victim, Entity aggressor, out AssistReason reason)
    {
        // group member with autoassist
        if (candidate.Has<GroupMember>() && victim.Has<GroupMember>())
        {
            if (candidate.Get<GroupMember>().Group == victim.Get<GroupMember>().Group)
                if (candidate.HasAutoAssist())
                {
                    reason = AssistReason.Group;
                    return true;
                }
        }

        // npc social assist
        if (candidate.Has<NpcAssistBehavior>())
        {
            ref var behavior = ref candidate.Get<NpcAssistBehavior>();
            if (ShouldNpcAssist(behavior, candidate, victim, aggressor))
            {
                reason = AssistReason.Npc;
                return true;
            }
        }

        reason = AssistReason.None;
        return false;
    }

    private void PropagateAssist(GameState state, Entity combatant, Entity aggressor)
    {
        // 1. group members with autoassist
        if (combatant.Has<GroupMember>())
        {
            var group = combatant.Get<GroupMember>().Group;
            ref var groupData = ref group.Get<Group>();
            foreach (var member in groupData.Members)
            {
                if (member == combatant) continue;
                if (!CharacterHelpers.SameRoom(combatant, member)) continue;
                if (!member.HasAutoAssist()) continue;
                TryAssist(member, aggressor, AssistReason.Group);
                // also pull in their charmies
                if (member.Has<Charmies>())
                    foreach (var charmie in member.Get<Charmies>().Entities)
                        if (CharacterHelpers.SameRoom(combatant, charmie))
                            TryAssist(charmie, aggressor, AssistReason.Charmed);
            }
        }

        // 2. charmies of the combatant auto-assist unconditionally
        if (combatant.Has<Charmies>())
        {
            foreach (var charmie in combatant.Get<Charmies>().Entities)
            {
                if (!CharacterHelpers.SameRoom(combatant, charmie)) continue;
                TryAssist(charmie, aggressor, AssistReason.Charmed);
            }
        }

        // 3. NPC social assist — same room scan
        ref var roomContents = ref combatant.Get<Location>().Room.Get<RoomContents>();
        foreach (var character in roomContents.Characters)
        {
            if (character == combatant || character == aggressor) continue;
            if (!character.Has<NpcAssistBehavior>()) continue;

            ref var behavior = ref character.Get<NpcAssistBehavior>();
            if (ShouldNpcAssist(behavior, character, combatant, aggressor))
                TryAssist(character, aggressor, AssistReason.Npc);
        }
    }

    private void TryAssist(Entity assistant, Entity target, AssistReason reason)
    {
        if (!CharacterHelpers.IsAlive(assistant)) return;
        if (assistant.Has<CombatState>()) return; // already fighting
        if (assistant == target) return;

        assistant.Add(new CombatState { Target = target, RoundDelay = 1 });
        assistant.Add<NewCombatantTag>(); // chain: this assist may trigger further assists
                                          // will be picked up by the NEXT AutoAssistSystem pass
    }

    private bool ShouldNpcAssist(NpcAssistBehavior behavior, Entity npc,
                                  Entity victim, Entity aggressor)
    {
        if (behavior.Flags.HasFlag(AssistFlags.GuardPlayers) && victim.Has<PlayerTag>()) return true;
        if (behavior.Flags.HasFlag(AssistFlags.SameRace) && SameRace(npc, victim)) return true;
        if (behavior.Flags.HasFlag(AssistFlags.SameClass) && SameClass(npc, victim)) return true;
        if (behavior.Flags.HasFlag(AssistFlags.SameAlign) && SameAlignment(npc, victim)) return true;
        if (behavior.Flags.HasFlag(AssistFlags.SameFaction) && SameFaction(npc, victim)) return true;
        return false;
    }

    private bool SameRace(Entity npc, Entity victim) // TODO
        => false;

    private bool SameClass(Entity npc, Entity victim) // TODO
        => false;

    private bool SameAlignment(Entity npc, Entity victim) // TODO
        => false;

    private bool SameFaction(Entity npc, Entity victim) // TODO
        => false;

    private enum AssistReason
    {
        None,
        Npc,
        Charmed,
        Group
    }
}
