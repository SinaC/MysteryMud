using Arch.Core.Extensions;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Calculators;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Combat.Resolvers;

public class HealResolver
{
    private readonly AggroResolver _aggroResolver;
    private readonly IGameMessageService _msg;

    public HealResolver(AggroResolver aggroResolver, IGameMessageService msg)
    {
        _aggroResolver = aggroResolver;
        _msg = msg;
    }

    public void Resolve(HealEvent heal) // to be used during combat process
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
    }
}
