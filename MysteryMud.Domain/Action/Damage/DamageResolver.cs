using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Action.Attack.Resolvers;
using MysteryMud.Domain.Action.Calculators;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Factories;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Action.Damage;

public class DamageResolver
{
    private readonly AggroResolver _aggroResolver;
    private readonly IGameMessageService _msg;
    private readonly IEventBuffer<DamagedEvent> _damaged;
    private readonly IEventBuffer<DeathEvent> _deaths;

    public DamageResolver(AggroResolver aggroResolver, IGameMessageService msg, IEventBuffer<DamagedEvent> damaged, IEventBuffer<DeathEvent> deaths)
    {
        _aggroResolver = aggroResolver;
        _msg = msg;
        _damaged = damaged;
        _deaths = deaths;
    }

    public void Resolve(GameState state, DamageAction dmg) // to be used during combat process
    {
        if (!dmg.Target.IsAlive() || dmg.Target.Has<Dead>()) // already dead
            return;

        ref var health = ref dmg.Target.Get<Health>();

        // apply damage type modifiers, resistances, vulnerabilities, etc.
        var modifiedDamage = DamageCalculator.ModifyDamage(dmg.Target, dmg.Amount, dmg.DamageKind, dmg.Source);
        // TODO: capping
        var finalDamage = modifiedDamage;
        // damage to apply, apply rounding
        var damageToApply = (int)Math.Round(finalDamage, MidpointRounding.AwayFromZero);

        // we have to split sending to source and sending to room because source may not be in the same room
        if (dmg.Source == dmg.Target)
            _msg.To(dmg.Source).Act("%rYou deal {0} damage to yourself.%x").With(damageToApply);
        else
        {
            _msg.To(dmg.Source).Act("%gYou deal {0} damage to {1}.%x").With(damageToApply, dmg.Target);
            _msg.To(dmg.Target).Act("%r{0} deal{0:v} {1} damage to you.%x").With(dmg.Source, damageToApply);
        }
        _msg.ToRoomExcept(dmg.Target, dmg.Source).Act("%y{0} deal{0:v} {1} damage to {2}.%x").With(dmg.Source, damageToApply, dmg.Target);

        // apply damage
        health.Current -= damageToApply;

        // generate aggro
        _aggroResolver.ResolveFromDamage(state, dmg.Target, dmg.Source, damageToApply
            , dmg.DamageKind);

        // check for death
        if (health.Current <= 0)
        {
            AddDeadTags(dmg.Target);

            health.Current = 0;

            ref var deathEvt = ref _deaths.Add();
            deathEvt.Dead = dmg.Target;
            deathEvt.Killer = dmg.Source;
        }

        // damaged event
        ref var damagedEvt = ref _damaged.Add();
        damagedEvt.Target = dmg.Target;
        damagedEvt.Source = dmg.Source;
        damagedEvt.Amount = damageToApply;
        damagedEvt.DamageKind = dmg.DamageKind;
        damagedEvt.SourceKind = dmg.SourceKind;
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
