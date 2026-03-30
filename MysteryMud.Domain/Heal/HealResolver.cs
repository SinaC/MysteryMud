using Arch.Core.Extensions;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Calculators;
using MysteryMud.Domain.Combat.Resolvers;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Actions;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Heal;

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

    public void Resolve(HealAction heal) // to be used during combat process
    {
        if (heal.Target.Has<Dead>()) // already dead
            return;

        ref var health = ref heal.Target.TryGetRef<Health>(out var hasHealth);
        if (!hasHealth)
            return;

        // apply heal modifiers
        var modifiedHeal = HealCalculator.ModifyHeal(heal.Target, heal.Amount, heal.Source);

        _msg.ToRoom(heal.Source).Act("%G{0} heal %g{1} for %g{2}%g health.%x").With(heal.Source, heal.Target, modifiedHeal);

        // apply heal
        health.Current += modifiedHeal;

        // generate aggro for healing
        _aggroResolver.ResolveFromHeal(heal.Target, heal.Source, modifiedHeal);

        // healed event
        ref var healedEvt = ref _healed.Add();
        healedEvt.Target = heal.Target;
        healedEvt.Source = heal.Source;
        healedEvt.Amount = heal.Amount;
        healedEvt.SourceType = heal.SourceType;
    }
}
