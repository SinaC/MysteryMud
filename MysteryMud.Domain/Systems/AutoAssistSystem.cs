using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class AutoAssistSystem
{
    private readonly World _world;
    private readonly IEventBuffer<RoomEnteredEvent> _roomEnteredEvent;

    public AutoAssistSystem(World world, IEventBuffer<RoomEnteredEvent> roomEnteredEvent)
    {
        _world = world;
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

    private static readonly QueryDescription _newCombatantQueryDescr = new QueryDescription()
        .WithAll<CombatState, NewCombatantTag>();

    private void ProcessNewCombatants(GameState state)
    {
        _world.Query(_newCombatantQueryDescr, (EntityId entity,
            ref CombatState combat,
            ref NewCombatantTag tag) =>
        {
            _world.Remove<NewCombatantTag>(entity); // consume immediately
            PropagateAssist(entity, combat.Target);
        });

    }

    private void ProcessRoomEntries(GameState state)
    {
        foreach (ref var evt in _roomEnteredEvent.GetAll())
            CheckAssistOnRoomEntry(ref evt);
    }

    private void CheckAssistOnRoomEntry(ref RoomEnteredEvent evt)
    {
        ref var roomContents = ref _world.Get<RoomContents>(evt.ToRoom);

        foreach (var character in roomContents.Characters)
        {
            if (character == evt.Entity) continue;
            if (!_world.Has<CombatState>(character)) continue;

            ref var combat = ref _world.Get<CombatState>(character);

            // check if entering entity should assist this combatant
            if (ShouldAssist(evt.Entity, character, combat.Target, out var reason))
            {
                TryAssist(evt.Entity, combat.Target, reason);
                // also check charmies of the entering entity
                if (_world.Has<Charmies>(evt.Entity))
                {
                    foreach (var charmie in _world.Get<Charmies>(evt.Entity).Entities)
                        TryAssist(charmie, combat.Target, AssistReason.Charmed);
                }
                return; // assist first eligible combatant only
            }
        }
    }

    private bool ShouldAssist(EntityId candidate, EntityId victim, EntityId aggressor, out AssistReason reason)
    {
        // group member with autoassist
        if (_world.Has<GroupMember>(candidate) && _world.Has<GroupMember>(victim))
        {
            if (_world.Get<GroupMember>(candidate).Group == _world.Get<GroupMember>(victim).Group)
                if (CharacterHelpers.HasAutoAssist(_world, candidate))
                {
                    reason = AssistReason.Group;
                    return true;
                }
        }

        // npc social assist
        if (_world.Has<NpcAssistBehavior>(candidate))
        {
            ref var behavior = ref _world.Get<NpcAssistBehavior>(candidate);
            if (ShouldNpcAssist(behavior, candidate, victim, aggressor))
            {
                reason = AssistReason.Npc;
                return true;
            }
        }

        reason = AssistReason.None;
        return false;
    }

    private void PropagateAssist(EntityId combatant, EntityId aggressor)
    {
        // 1. group members with autoassist
        if (_world.Has<GroupMember>(combatant))
        {
            var group = _world.Get<GroupMember>(combatant).Group;
            ref var groupData = ref _world.Get<GroupInstance>(group);
            foreach (var member in groupData.Members)
            {
                if (member == combatant) continue;
                if (!CharacterHelpers.SameRoom(_world, combatant, member)) continue;
                if (!CharacterHelpers.HasAutoAssist(_world, member)) continue;
                TryAssist(member, aggressor, AssistReason.Group);
                // also pull in their charmies
                if (_world.Has<Charmies>(member))
                    foreach (var charmie in _world.Get<Charmies>(member).Entities)
                        if (CharacterHelpers.SameRoom(_world, combatant, charmie))
                            TryAssist(charmie, aggressor, AssistReason.Charmed);
            }
        }

        // 2. charmies of the combatant auto-assist unconditionally
        if (_world.Has<Charmies>(combatant))
        {
            foreach (var charmie in _world.Get<Charmies>(combatant).Entities)
            {
                if (!Helpers.CharacterHelpers.SameRoom(_world, combatant, charmie)) continue;
                TryAssist(charmie, aggressor, AssistReason.Charmed);
            }
        }

        // 3. NPC social assist — same room scan
        ref var room = ref _world.Get<Location>(combatant).Room;
        ref var roomContents = ref _world.Get<RoomContents>(room);
        foreach (var character in roomContents.Characters)
        {
            if (character == combatant || character == aggressor) continue;
            if (!_world.Has<NpcAssistBehavior>(character)) continue;

            ref var behavior = ref _world.Get<NpcAssistBehavior>(character);
            if (ShouldNpcAssist(behavior, character, combatant, aggressor))
                TryAssist(character, aggressor, AssistReason.Npc);
        }
    }

    private void TryAssist(EntityId assistant, EntityId target, AssistReason reason)
    {
        if (!CharacterHelpers.IsAlive(_world, assistant)) return;
        if (_world.Has<CombatState>(assistant)) return; // already fighting
        if (assistant == target) return;

        _world.Add(assistant,  new CombatState { Target = target, RoundDelay = 1 });
        if (!_world.Has<NewCombatantTag>(assistant))
            _world.Add<NewCombatantTag>(assistant); // chain: this assist may trigger further assists
                                                    // will be picked up by the NEXT AutoAssistSystem pass
    }

    private bool ShouldNpcAssist(NpcAssistBehavior behavior, EntityId npc,
                                  EntityId victim, EntityId aggressor)
    {
        if (behavior.Flags.HasFlag(AssistFlags.GuardPlayers) && _world.Has<PlayerTag>(victim)) return true;
        if (behavior.Flags.HasFlag(AssistFlags.SameRace) && SameRace(npc, victim)) return true;
        if (behavior.Flags.HasFlag(AssistFlags.SameClass) && SameClass(npc, victim)) return true;
        if (behavior.Flags.HasFlag(AssistFlags.SameAlign) && SameAlignment(npc, victim)) return true;
        if (behavior.Flags.HasFlag(AssistFlags.SameFaction) && SameFaction(npc, victim)) return true;
        return false;
    }

    private bool SameRace(EntityId npc, EntityId victim) // TODO
        => false;

    private bool SameClass(EntityId npc, EntityId victim) // TODO
        => false;

    private bool SameAlignment(EntityId npc, EntityId victim) // TODO
        => false;

    private bool SameFaction(EntityId npc, EntityId victim) // TODO
        => false;

    private enum AssistReason
    {
        None,
        Npc,
        Charmed,
        Group
    }
}
