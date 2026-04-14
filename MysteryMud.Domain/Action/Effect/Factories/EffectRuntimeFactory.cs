using MysteryMud.Domain.Action.Effect.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Factories;

public class EffectRuntimeFactory : IEffectRuntimeFactory
{
    private IEffectActionFactory _effectActionFactory;

    public EffectRuntimeFactory(IEffectActionFactory effectActionFactory)
    {
        _effectActionFactory = effectActionFactory;
    }

    public EffectRuntime Create(EffectDefinition def)
    {
        var onApply = new List<Action<EffectExecutionContext>>();
        var onTick = new List<Action<EffectExecutionContext>>();
        var onExpire = new List<Action<EffectExecutionContext>>();

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
            onExpire.Add(ctx => ctx.Msg.To(ctx.Context.Target).Send(def.WearOffMessage));
        // apply message
        if (def.ApplyMessage != null)
            onApply.Add(ctx => ctx.Msg.To(ctx.Context.Target).Send(def.ApplyMessage));

        if (def.DurationCompiledFormula == null && (onTick.Count > 0 || onExpire.Count > 0))
            throw new Exception($"DurationFormula must be specified when Trigger OnTick or OnExpire is defined in effect '{def.Name}'");

        if (def.TickRate == 0 && onTick.Count > 0)
            throw new Exception($"TickRate cannot be 0 when Trigger OnTick is defined in effect '{def.Name}'");

        if (def.TickRate > 0 && onTick.Count == 0)
            throw new Exception($"TickRate is 0 but no Trigger OnTick is defined in effect '{def.Name}'");

        if (def.Actions.Any(x => x is CharacterStatModifierActionDefinition) && def.DurationCompiledFormula == null)
            throw new Exception($"DurationFormula must be specified if a StatModifierAction is defined in effect '{def.Name}'");

        if (def.Actions.Any(x => x is HealthModifierActionDefinition or ManaModifierActionDefinition or EnergyModifierActionDefinition or RageModifierActionDefinition) && def.DurationCompiledFormula == null)
            throw new Exception($"DurationFormula must be specified if a ResourceModifierAction is defined in effect '{def.Name}'");

        return new EffectRuntime
        {
            Id = def.Id,
            Name = def.Name,
            Tag = def.Tag,
            Stacking = def.Stacking,
            MaxStacks = def.MaxStacks,

            DurationFunc = def.DurationCompiledFormula?.Compiled,

            TickOnApply = def.TickOnApply,
            TickRate = def.TickRate,

            OnApply = onApply.ToArray(),
            OnTick = onTick.ToArray(),
            OnExpire = onExpire.ToArray(),
        };
    }
}
