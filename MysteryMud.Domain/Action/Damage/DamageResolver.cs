using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Effects;
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
    private readonly IEventBuffer<DeathEvent> _deaths; // long-term events which will be used by other Systems outside ActionOrchestrator
    private readonly IEventBuffer<KillRewardEvent> _killRewardEvent; // short-term events only used inside ActionOrchestrator

    public DamageResolver(AggroResolver aggroResolver, IGameMessageService msg, IEventBuffer<DamagedEvent> damaged, IEventBuffer<DeathEvent> deaths, IEventBuffer<KillRewardEvent> killRewardEvent)
    {
        _aggroResolver = aggroResolver;
        _msg = msg;
        _damaged = damaged;
        _deaths = deaths;
        _killRewardEvent = killRewardEvent;
    }

    public DamageResult Resolve(GameState state, DamageAction dmg) // to be used during combat process
    {
        var source = dmg.Source;
        var target = dmg.Target;
        var amount = dmg.Amount;
        var kind = dmg.DamageKind;
        if (!target.IsAlive() || target.Has<Dead>()) // already dead
            return new DamageResult { IsSuccess = false };

        ref var health = ref target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return new DamageResult { IsSuccess = false };

        // apply damage type modifiers, resistances, vulnerabilities, etc.
        var modifiedDamage = DamageCalculator.ModifyDamage(target, amount, kind, source);
        // TODO: capping
        var finalDamage = modifiedDamage;
        // damage to apply, apply rounding
        var damageToApply = (int)Math.Round(finalDamage, MidpointRounding.AwayFromZero);

        // we have to split sending to source and sending to room because source may not be in the same room
        if (source == target)
            _msg.To(source).Act("%rYou deal {0} damage to yourself.%x").With(damageToApply);
        else
        {
            _msg.To(source).Act("%gYou deal {0} damage to {1}.%x").With(damageToApply, target);
            _msg.To(target).Act("%r{0} deal{0:v} {1} damage to you.%x").With(source, damageToApply);
        }
        _msg.ToRoomExcept(target, source).Act("%y{0} deal{0:v} {1} damage to {2}.%x").With(source, damageToApply, target);

        // apply damage
        health.Current -= damageToApply;

        // generate aggro
        _aggroResolver.ResolveFromDamage(state, target, source, damageToApply, kind);

        // check for death
        var killed = false;
        if (health.Current <= 0)
        {
            killed = true;
            _msg.To(target).Send("%RYou have been KILLED%x");
            _msg.ToRoom(target).Act("{0} is dead").With(target);

            AddDeadTags(target);

            health.Current = 0;

            // add death event
            ref var deathEvt = ref _deaths.Add();
            deathEvt.Victim = target;
            deathEvt.Killer = source;

            // add kill reward event
            ref var killRewardEvt = ref _killRewardEvent.Add();
            killRewardEvt.Killer = source;
            killRewardEvt.Victim = target;
            killRewardEvt.GrantXp = true;
            killRewardEvt.GrantLoop = false;
        }

        // damaged event
        ref var damagedEvt = ref _damaged.Add();
        damagedEvt.Target = target;
        damagedEvt.Source = source;
        damagedEvt.Amount = damageToApply;
        damagedEvt.DamageKind = kind;
        damagedEvt.SourceKind = dmg.SourceKind;

        return new DamageResult
        {
            IsSuccess = true,
            Amount = dmg.Amount,
            Killed = killed,
            EffectiveAmount = damageToApply
        };
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
