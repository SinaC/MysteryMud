using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using MysteryMud.Domain.Action.Effect.Definitions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
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
        // Infer which target kinds this effect supports based on its actions
        var supportedTargets = def.Actions
            .Select(EffectActionRegistry.GetAllowedTargets)
            .Aggregate((a, b) => a & b); // intersection: all actions must support the target

        if (supportedTargets == 0)
            throw new Exception(
                $"Effect '{def.Name}' has incompatible actions: no target kind satisfies all of them. " +
                $"Check that character-only and item-only actions are not mixed.");


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
            onExpire.Add(CreateDisplayMessageAction(def.WearOffMessage));
        // apply message
        if (def.ApplyMessage != null)
            onApply.Add(CreateDisplayMessageAction(def.ApplyMessage));

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

            SupportedTargets = supportedTargets,

            DurationFunc = def.DurationCompiledFormula?.Compiled,
            IsHarmful = def.IsHarmful,

            Tag = def.Tag,
            Stacking = def.Stacking,
            MaxStacks = def.MaxStacks,

            TickOnApply = def.TickOnApply,
            TickRate = def.TickRate,

            OnApply = onApply.ToArray(),
            OnTick = onTick.ToArray(),
            OnExpire = onExpire.ToArray(),
        };
    }

    private Action<EffectExecutionContext> CreateDisplayMessageAction(string msg)
    {
        return ctx =>
        {
            var target = ctx.Context.Target;
            if (target.Has<CharacterTag>())
                ctx.Msg.To(target).Send(msg);
            else
            {
                if (target.TryGet<Equipped>(out var equipped))
                    ctx.Msg.To(equipped.Wearer).Act(msg).With(target);
                else if (target.TryGet<ContainedIn>(out var containedIn) && containedIn.Character.Has<CharacterTag>())
                    ctx.Msg.To(containedIn.Character).Act(msg).With(target);
            }
        };
    }
}
