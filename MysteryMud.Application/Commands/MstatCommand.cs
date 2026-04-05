using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class MstatCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            executionContext.Msg.To(actor).Send("Mstat what ?");
            return;
        }

        var people = actor.Get<Location>().Room.Get<RoomContents>().Characters;

        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary, people);
        if (target == default)
        {
            executionContext.Msg.To(actor).Send("No such target.");
            return;
        }

        // TODO: ref ?
        var (name, location, health, baseStats, effectiveStats, inventory, equipment, characterEffects) = target.Get<Name, Location, Health, BaseStats, EffectiveStats, Inventory, Equipment, CharacterEffects>();
        executionContext.Msg.To(actor).Send($"Name: {name.Value}");
        ref var description = ref target.TryGetRef<Description>(out var hasDescription);
        if (hasDescription)
            executionContext.Msg.To(actor).Send($"Description: {description.Value}");
        executionContext.Msg.To(actor).Send($"Location: {location.Room.DisplayName}");
        executionContext.Msg.To(actor).Send($"Health: {health.Current}/{health.Max}");
        ref var mana = ref target.TryGetRef<Mana>(out var hasMana);
        if (hasMana)
            executionContext.Msg.To(actor).Send($"Mana: {mana.Current}/{mana.Max}");
        foreach (var stat in Enum.GetValues<StatKind>())
        {
            executionContext.Msg.To(actor).Send($"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        ref var combatState = ref target.TryGetRef<CombatState>(out var inCombat);
        if (inCombat)
            executionContext.Msg.To(actor).Send($"Fighting: {combatState.Target.DisplayName} Delay: {combatState.RoundDelay}");
        executionContext.Msg.To(actor).Send($"Inventory:");
        foreach (var item in inventory.Items)
            executionContext.Msg.To(actor).Send($"- {item.DisplayName}");
        executionContext.Msg.To(actor).Send($"Equipment:");
        foreach (var slot in Enum.GetValues<EquipmentSlotKind>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                executionContext.Msg.To(actor).Send($"{slot}: {item.DisplayName}");
            else
                executionContext.Msg.To(actor).Send($"{slot}: nothing");
        }
        executionContext.Msg.To(actor).Send($"Active tags: {characterEffects.ActiveTags}");
        executionContext.Msg.To(actor).Send($"Effects:");
        foreach (var effect in characterEffects.Effects)
        {
            if (!effect.IsAlive() || effect.Has<ExpiredTag>())
                continue;
            ref var effectInstance = ref effect.Get<EffectInstance>();
            if (effectInstance.EffectRuntime != null)
            {
                // TODO: how could we display hot/dot
                var effectName = effectInstance.EffectRuntime.Name;
                var stackCount = effectInstance.StackCount;
                var sourceName = effectInstance.Source.DisplayName;

                ref var timedEffect = ref effect.TryGetRef<TimedEffect>(out var isTimedEffect);
                if (isTimedEffect)
                {
                    var remainingTicks = timedEffect.ExpirationTick - state.CurrentTick;
                    if (timedEffect.TickRate > 0)
                        executionContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining ticks: {remainingTicks} Tick rate: {timedEffect.TickRate}");
                    else
                        executionContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining ticks: {remainingTicks}");
                }
                else
                    executionContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Permanent");

                ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
                if (hasStatModifiers)
                {
                    foreach (var modifier in statModifiers.Values)
                        executionContext.Msg.To(actor).Send($"  - {modifier.Modifier} {modifier.Value} {modifier.Stat}");
                }
            }
        }
    }
}
