using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Domain.Components.Extensions;
using MysteryMud.ConsoleApp3.Systems;
using MysteryMud.ConsoleApp3.Domain.Components;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;
using MysteryMud.ConsoleApp3.Domain.Components.Effects;
using MysteryMud.ConsoleApp3.Domain.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Commands;

public class MstatCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        var people = actor.Get<Location>().Room.Get<RoomContents>().Characters;

        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Primary, people);
        if (target == default)
        {
            systemContext.MessageBus.Publish(actor, "No such target.");
            return;
        }

        // TODO: ref ?
        var (name, location, health, baseStats, effectiveStats, inventory, equipment, characterEffects) = target.Get<Name, Location, Health, BaseStats, EffectiveStats, Inventory, Equipment, CharacterEffects>();
        systemContext.MessageBus.Publish(actor, $"Name: {name.Value}");
        ref var description = ref target.TryGetRef<Description>(out var hasDescription);
        if (hasDescription)
            systemContext.MessageBus.Publish(actor, $"Description: {description.Value}");
        systemContext.MessageBus.Publish(actor, $"Location: {location.Room.DisplayName}");
        systemContext.MessageBus.Publish(actor, $"Health: {health.Current}/{health.Max}");
        ref var mana = ref target.TryGetRef<Mana>(out var hasMana);
        if (hasMana)
            systemContext.MessageBus.Publish(actor, $"Mana: {mana.Current}/{mana.Max}");
        foreach (var stat in Enum.GetValues<StatType>())
        {
            systemContext.MessageBus.Publish(actor, $"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        ref var combatState = ref target.TryGetRef<CombatState>(out var inCombat);
        if (inCombat)
            systemContext.MessageBus.Publish(actor, $"Fighting: {combatState.Target.DisplayName} Delay: {combatState.RoundDelay}");
        systemContext.MessageBus.Publish(actor, $"Inventory:");
        foreach (var item in inventory.Items)
            systemContext.MessageBus.Publish(actor, $"- {item.DisplayName}");
        systemContext.MessageBus.Publish(actor, $"Equipment:");
        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                systemContext.MessageBus.Publish(actor, $"{slot}: {item.DisplayName}");
            else
                systemContext.MessageBus.Publish(actor, $"{slot}: nothing");
        }
        systemContext.MessageBus.Publish(actor, $"Active tags: {characterEffects.ActiveTags}");
        systemContext.MessageBus.Publish(actor, $"Effects:");
        foreach (var effect in characterEffects.Effects)
        {
            ref var effectInstance = ref effect.Get<EffectInstance>();
            ref var duration = ref effect.TryGetRef<Duration>(out var hasDuration);
            ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
            ref var damageOverTime = ref effect.TryGetRef<DamageOverTime>(out var hasDamageOverTime);
            ref var healOverTime = ref effect.TryGetRef<HealOverTime>(out var hasHealOverTime);
            var effectName = effectInstance.Template.Name;
            var stackCount = effectInstance.StackCount;
            var sourceName = effectInstance.Source.DisplayName;
            if (hasDuration)
            {
                var remainingTicks = duration.ExpirationTick - (duration.LastRefreshTick ?? duration.StartTick);
                systemContext.MessageBus.Publish(actor, $"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining ticks: {remainingTicks}");
            }
            else
                systemContext.MessageBus.Publish(actor, $"- {effectName} Source: {sourceName} Stacks: {stackCount} Permanent");
            if (hasStatModifiers)
            {
                foreach (var modifier in statModifiers.Values)
                    systemContext.MessageBus.Publish(actor, $"  - {modifier.Type} {modifier.Value} {modifier.Stat}");
            }
            if (hasDamageOverTime)
                systemContext.MessageBus.Publish(actor, $"  - Damage over time: {damageOverTime.Damage} every {damageOverTime.TickRate} ticks");
            if (hasHealOverTime)
                systemContext.MessageBus.Publish(actor, $"  - Heal over time: {healOverTime.Heal} every {healOverTime.TickRate} ticks");
        }
    }
}
