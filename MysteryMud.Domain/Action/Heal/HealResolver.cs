using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Effects;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Action.Attack.Resolvers;
using MysteryMud.Domain.Action.Calculators;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Action.Heal;

public class HealResolver
{
    private readonly AggroResolver _aggroResolver;
    private readonly IGameMessageService _msg;
    private readonly IEventBuffer<HealedEvent> _healed;

    public HealResolver(AggroResolver aggroResolver, IGameMessageService msg, IEventBuffer<HealedEvent> healed)
    {
        _aggroResolver = aggroResolver;
        _msg = msg;
        _healed = healed;
    }

    public void Resolve(GameState state, HealAction heal) // to be used during combat process
    {
        if (!heal.Target.IsAlive() || heal.Target.Has<Dead>()) // already dead
            return;

        ref var health = ref heal.Target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return;

        if (health.Current >= health.Max) // already at max hp
            return;

        // apply heal modifiers
        decimal modifiedHeal = HealCalculator.ModifyHeal(heal.Target, heal.Amount, heal.Source);
        // cap to max health-current
        decimal maxPossibleHeal = health.Max - health.Current;
        decimal finalHeal = Math.Min(modifiedHeal, maxPossibleHeal);
        // heal to apply, apply rounding
        int healToApply = (int)Math.Round(finalHeal, MidpointRounding.AwayFromZero);
        health.Current += healToApply;

        // we have to split sending to source and sending to room because source may not be in the same room
        if (heal.Source == heal.Target)
            _msg.To(heal.Source).Act("%gYou heal yourself for {0} health.%x").With(healToApply);
        else
        {
            _msg.To(heal.Source).Act("%gYou heal {0:n} for {1} health.%x").With(heal.Target, healToApply);
            _msg.To(heal.Target).Act("%g{0} heal{0:v} you for {1} health.%x").With(heal.Source, healToApply);
            _msg.ToRoomExcept(heal.Target, heal.Source).Act("%y{0} heal{0:v} {1} for {2} health.%x").With(heal.Source, heal.Target, healToApply);
        }

        // apply heal
        health.Current = health.Current + healToApply;

        // generate aggro for healing
        _aggroResolver.ResolveFromHeal(state, heal.Target, heal.Source, healToApply);

        // healed event
        ref var healedEvt = ref _healed.Add();
        healedEvt.Target = heal.Target;
        healedEvt.Source = heal.Source;
        healedEvt.Amount = healToApply;
        healedEvt.SourceKind = heal.SourceKind;
    }
}
