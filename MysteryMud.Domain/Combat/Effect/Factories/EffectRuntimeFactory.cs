using MysteryMud.Domain.Combat.Effect.Definitions;
using MysteryMud.GameData.Enums;
using System.Linq;

namespace MysteryMud.Domain.Combat.Effect.Factories;

public class EffectRuntimeFactory
{
    private EffectActionFactory _effectActionFactory;

    public EffectRuntimeFactory(EffectActionFactory effectActionFactory)
    {
        _effectActionFactory = effectActionFactory;
    }

    public EffectRuntime Create(EffectDefinition def)
    {
        var onApply = new List<Action<EffectContext>>();
        var onTick = new List<Action<EffectContext>>();
        var onExpire = new List<Action<EffectContext>>();

        foreach (var actionData in def.Actions)
        {
            var action = _effectActionFactory.Create(actionData);

            // sort by trigger
            switch (actionData.Trigger)
            {
                case TriggerType.OnApply:
                    onApply.Add(action);
                    break;
                case TriggerType.OnTick:
                    onTick.Add(action);
                    break;
                case TriggerType.OnExpire:
                    onExpire.Add(action);
                    break;
                default:
                    throw new NotSupportedException($"Unknown trigger '{actionData.Trigger}' in effect '{def.Name}'");
            }
        }

        // wear off message (add OnExpire action)
        if (def.WearOffMessage != null)
            onExpire.Add(ctx => ctx.Msg.To(ctx.Target).Send(def.WearOffMessage));
        // apply message
        if (def.ApplyMessage != null)
            onApply.Add(ctx => ctx.Msg.To(ctx.Target).Send(def.ApplyMessage));

        if (def.DurationFunc == null && (onTick.Count > 0 || onExpire.Count > 0))
            throw new Exception($"DurationFormula must be specified when Trigger OnTick or OnExpire is defined in effect '{def.Name}'");

        if (def.TickRate == 0 && onTick.Count > 0)
            throw new Exception($"TickRate cannot be 0 when Trigger OnTick is defined in effect '{def.Name}'");

        if (def.Actions.Any(x => x is StatModifierActionDefinition) && def.DurationFunc == null)
            throw new Exception($"DurationFormula must be specified if a StatModifierAction is defined in effect '{def.Name}'");

        if (def.Actions.Any(x => x is HealthModifierActionDefinition or ManaModifierActionDefinition or EnergyModifierActionDefinition or RageModifierActionDefinition) && def.DurationFunc == null)
            throw new Exception($"DurationFormula must be specified if a ResourceModifierAction is defined in effect '{def.Name}'");

        return new EffectRuntime
        {
            Id = def.Id,
            Name = def.Name,
            Tag = def.Tag,
            Stacking = def.Stacking,
            MaxStacks = def.MaxStacks,

            DurationFunc = def.DurationFunc,

            TickOnApply = def.TickOnApply,
            TickRate = def.TickRate,

            OnApply = onApply.ToArray(),
            OnTick = onTick.ToArray(),
            OnExpire = onExpire.ToArray(),
        };
    }
}
