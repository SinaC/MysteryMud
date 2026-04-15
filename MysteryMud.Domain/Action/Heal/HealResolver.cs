using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Effects;
using MysteryMud.Domain.Action.Attack.Resolvers;
using MysteryMud.Domain.Action.Calculators;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Action.Heal;

public class HealResolver : IHealResolver
{
    private readonly IAggroResolver _aggroResolver;
    private readonly IGameMessageService _msg;
    private readonly IEventBuffer<HealedEvent> _healed;

    public HealResolver(IAggroResolver aggroResolver, IGameMessageService msg, IEventBuffer<HealedEvent> healed)
    {
        _aggroResolver = aggroResolver;
        _msg = msg;
        _healed = healed;
    }

    public HealResult Resolve(GameState state, HealAction heal) // to be used during combat process
    {
        var source = heal.Source;
        var target = heal.Target;
        var amount = heal.Amount;
        if (!target.IsAlive() || target.Has<Dead>()) // already dead
            return new HealResult { IsSuccess = false };

        ref var health = ref target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return new HealResult { IsSuccess = false };

        if (health.Current >= health.Max) // already at max hp
            return new HealResult
            {
                IsSuccess = false,
                MaxHealth = true
            };

        // apply heal modifiers
        decimal modifiedHeal = HealCalculator.ModifyHeal(target, amount, source);
        // cap to max health-current
        decimal maxPossibleHeal = health.Max - health.Current;
        decimal finalHeal = Math.Min(modifiedHeal, maxPossibleHeal);
        // heal to apply, apply rounding
        int healToApply = (int)Math.Round(finalHeal, MidpointRounding.AwayFromZero);

        // we have to split sending to source and sending to room because source may not be in the same room
        if (source == target)
            _msg.To(heal.Source).Act("%gYou heal yourself for {0} health.%x").With(healToApply);
        else
        {
            _msg.To(source).Act("%gYou heal {0:n} for {1} health.%x").With(target, healToApply);
            _msg.To(target).Act("%g{0} heal{0:v} you for {1} health.%x").With(source, healToApply);
            _msg.ToRoomExcept(target, source).Act("%y{0} heal{0:v} {1} for {2} health.%x").With(source, target, healToApply);
        }

        // apply heal
        health.Current += healToApply;

        // generate aggro for healing
        _aggroResolver.ResolveFromHeal(state, target, source, healToApply);

        // healed event
        ref var healedEvt = ref _healed.Add();
        healedEvt.Target = target;
        healedEvt.Source = source;
        healedEvt.Amount = healToApply;
        healedEvt.SourceKind = heal.SourceKind;

        return new HealResult
        {
            IsSuccess = true,
            Amount = amount,
            MaxHealth = health.Current == health.Max,
            EffectiveAmount = healToApply
        };
    }
}
