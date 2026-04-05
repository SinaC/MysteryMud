using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Combat.Attack.Resolvers;
using MysteryMud.Domain.Combat.Calculators;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Combat.Heal;

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
        var modifiedHeal = HealCalculator.ModifyHeal(heal.Target, heal.Amount, heal.Source);
        // cap to max health-current
        var finalHeal = Math.Min(modifiedHeal, health.Max - health.Current);

        // we have to split sending to source and sending to room because source may not be in the same room
        _msg.To(heal.Source).Act("%gYou heal {0} for {1} health.%x").With(heal.Target, finalHeal);
        _msg.To(heal.Target).Act("%gYou heal {0} for {1} health.%x").With(heal.Target, finalHeal);
        _msg.ToRoomExcept(heal.Target, heal.Source).Act("%y{0} heal{0:v} {1} for {2} health.%x").With(heal.Source, heal.Target, finalHeal);

        // apply heal
        health.Current = health.Current + finalHeal;

        // generate aggro for healing
        _aggroResolver.ResolveFromHeal(state, heal.Target, heal.Source, finalHeal);

        // healed event
        ref var healedEvt = ref _healed.Add();
        healedEvt.Target = heal.Target;
        healedEvt.Source = heal.Source;
        healedEvt.Amount = finalHeal;
        healedEvt.SourceKind = heal.SourceKind;
    }
}
