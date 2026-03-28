using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Factories;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Combat.Resolvers;

public class DamageResolver
{
    private readonly IGameMessageService _msg;
    private readonly IEventBuffer<DeathEvent> _deaths;

    public DamageResolver(IGameMessageService msg, IEventBuffer<DeathEvent> deaths)
    {
        _msg = msg;
        _deaths = deaths;
    }

    public void Resolve(DamageEvent dmg) // to be used during combat process
    {
        if (dmg.Target.Has<Dead>()) // already dead
            return;

        ref var health = ref dmg.Target.Get<Health>();

        _msg.ToAll(dmg.Source).Act("%G{0} deal{0:v} %r{1}%g damage to {2}.%x").With(dmg.Source, dmg.Amount, dmg.Target);

        health.Current -= dmg.Amount;
        if (health.Current <= 0)
        {
            AddDeadTags(dmg.Target);

            health.Current = 0;

            ref var deathEvt = ref _deaths.Add();
            deathEvt.Dead = dmg.Target;
            deathEvt.Killer = dmg.Source;
        }
    }

    private static void AddDeadTags(Entity victim)
    {
        victim.Add<Dead>();
        // player will respawn, NPCs will be cleaned up by CleanupSystem
        if (victim.Has<PlayerTag>())
            victim.Add(new RespawnState
            {
                RespawnRoom = RoomFactory.RespawnRoomEntity
            });
    }
}
