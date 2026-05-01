using DefaultEcs;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class NPCTargetSystem
{
    private readonly ILogger _logger;
    private readonly IEventBuffer<AggressedEvent> _aggressed;
    private readonly EntitySet _activeThreatNpcsEntitySet;

    public NPCTargetSystem(World world, ILogger logger, IEventBuffer<AggressedEvent> aggressed)
    {
        _logger = logger;
        _aggressed = aggressed;
        _activeThreatNpcsEntitySet = world
            .GetEntities()
            .With<ThreatTable>()
            .With<ActiveThreatTag>()
            .With<NpcTag>()
            .AsSet();
    }

    public void Tick(GameState state)
    {
        foreach(var npc in _activeThreatNpcsEntitySet.GetEntities())
        {
            ref var threatTable = ref npc.Get<ThreatTable>();

            var npcRoom = npc.Get<Location>().Room;
            Entity bestTarget = default;
            decimal bestThreat = 0;

            foreach (var (entity, threat) in threatTable.Entries)
            {
                if (threat > bestThreat &&
                    entity.Has<Location>() &&
                    entity.Get<Location>().Room == npcRoom)
                {
                    bestThreat = threat;
                    bestTarget = entity;
                }
            }

            if (bestTarget == default)
                return; // no valid targets in the room, so we can skip this NPC for now.

            // if already in combat, switch to the target with the highest threat if it's different from the current target.
            if (npc.Has<CombatState>())
            {
                ref var combatState = ref npc.Get<CombatState>();
                if (combatState.Target != bestTarget)
                {
                    _logger.LogInformation("NPC {NpcId} is changing target from {OldTarget} to {NewTarget} with threat value {ThreatValue}", npc.DebugName, combatState.Target.DebugName, bestTarget.DebugName, bestThreat);
                    combatState.Target = bestTarget;
                }
            }
            // if not in combat, start attacking the target with the highest threat.
            else
            {
                _logger.LogInformation("NPC {NpcId} is starting combat with {NewTarget} with threat value {ThreatValue}", npc.DebugName, bestTarget.DebugName, bestThreat);

                ref var aggressedEvt = ref _aggressed.Add();
                aggressedEvt.Source = npc;
                aggressedEvt.Target = bestTarget;
            }
        }
    }
}
