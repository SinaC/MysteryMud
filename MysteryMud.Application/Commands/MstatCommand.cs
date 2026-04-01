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
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class MstatCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    public CommandDefinition Definition { get; }

    public MstatCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Mstat what ?");
            return;
        }

        var people = actor.Get<Location>().Room.Get<RoomContents>().Characters;

        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary, people);
        if (target == default)
        {
            systemContext.Msg.To(actor).Send("No such target.");
            return;
        }

        // TODO: ref ?
        var (name, location, health, baseStats, effectiveStats, inventory, equipment, characterEffects) = target.Get<Name, Location, Health, BaseStats, EffectiveStats, Inventory, Equipment, CharacterEffects>();
        systemContext.Msg.To(actor).Send($"Name: {name.Value}");
        ref var description = ref target.TryGetRef<Description>(out var hasDescription);
        if (hasDescription)
            systemContext.Msg.To(actor).Send($"Description: {description.Value}");
        systemContext.Msg.To(actor).Send($"Location: {location.Room.DisplayName}");
        systemContext.Msg.To(actor).Send($"Health: {health.Current}/{health.Max}");
        ref var mana = ref target.TryGetRef<Mana>(out var hasMana);
        if (hasMana)
            systemContext.Msg.To(actor).Send($"Mana: {mana.Current}/{mana.Max}");
        foreach (var stat in Enum.GetValues<StatKind>())
        {
            systemContext.Msg.To(actor).Send($"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        ref var combatState = ref target.TryGetRef<CombatState>(out var inCombat);
        if (inCombat)
            systemContext.Msg.To(actor).Send($"Fighting: {combatState.Target.DisplayName} Delay: {combatState.RoundDelay}");
        systemContext.Msg.To(actor).Send($"Inventory:");
        foreach (var item in inventory.Items)
            systemContext.Msg.To(actor).Send($"- {item.DisplayName}");
        systemContext.Msg.To(actor).Send($"Equipment:");
        foreach (var slot in Enum.GetValues<EquipmentSlotKind>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                systemContext.Msg.To(actor).Send($"{slot}: {item.DisplayName}");
            else
                systemContext.Msg.To(actor).Send($"{slot}: nothing");
        }
        systemContext.Msg.To(actor).Send($"Active tags: {characterEffects.ActiveTags}");
        systemContext.Msg.To(actor).Send($"Effects:");
        foreach (var effect in characterEffects.Effects)
        {
            if (!effect.IsAlive() || effect.Has<ExpiredTag>())
                continue;
            ref var effectInstance = ref effect.Get<EffectInstance>();
            ref var timedEffect = ref effect.TryGetRef<TimedEffect>(out var isTimedEffect);
            ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
            ref var damageEffect = ref effect.TryGetRef<DamageEffect>(out var hasDamageEffect);
            ref var healEffect = ref effect.TryGetRef<HealEffect>(out var hasHealEffect);
            var effectName = effectInstance.Definition.Id;
            var stackCount = effectInstance.StackCount;
            var sourceName = effectInstance.Source.DisplayName;
            if (isTimedEffect)
            {
                var remainingTicks = timedEffect.ExpirationTick - state.CurrentTick;
                if (timedEffect.TickRate > 0)
                    systemContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining ticks: {remainingTicks} Tick rate: {timedEffect.TickRate}");
                else
                    systemContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining ticks: {remainingTicks}");
            }
            else
                systemContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Permanent");
            if (hasStatModifiers)
            {
                foreach (var modifier in statModifiers.Values)
                    systemContext.Msg.To(actor).Send($"  - {modifier.Kind} {modifier.Value} {modifier.Stat}");
            }
            if (hasDamageEffect)
                systemContext.Msg.To(actor).Send($"  - Damage over time: {damageEffect.Damage} by stack");
            if (hasHealEffect)
                systemContext.Msg.To(actor).Send($"  - Heal over time: {healEffect.Heal} by stack");
        }
    }
}
