using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class MstatCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        var people = actor.Get<Location>().Room.Get<RoomContents>().Characters;

        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Primary, people);
        if (target == default)
        {
            MessageSystem.Send(actor, "No such target.");
            return;
        }

        // TODO: ref ?
        var (name, location, health, baseStats, effectiveStats, inventory, equipment, characterEffects) = target.Get<Name, Location, Health, BaseStats, EffectiveStats, Inventory, Equipment, CharacterEffects>();
        MessageSystem.Send(actor, $"Name: {name.Value}");
        ref var description = ref target.TryGetRef<Description>(out var hasDescription);
        if (hasDescription)
            MessageSystem.Send(actor, $"Description: {description.Value}");
        MessageSystem.Send(actor, $"Location: {location.Room.DisplayName}");
        MessageSystem.Send(actor, $"Health: {health.Current}/{health.Max}");
        ref var mana = ref target.TryGetRef<Mana>(out var hasMana);
        if (hasMana)
            MessageSystem.Send(actor, $"Mana: {mana.Current}/{mana.Max}");
        foreach (var stat in Enum.GetValues<StatType>())
        {
            MessageSystem.Send(actor, $"{stat}: {effectiveStats.Values[stat]}/{baseStats.Values[stat]}");
        }
        ref var combatState = ref target.TryGetRef<CombatState>(out var inCombat);
        if (inCombat)
            MessageSystem.Send(actor, $"Fighting: {combatState.Target.DisplayName} Delay: {combatState.RoundDelay}");
        MessageSystem.Send(actor, $"Inventory:");
        foreach (var item in inventory.Items)
            MessageSystem.Send(actor, $"- {item.DisplayName}");
        MessageSystem.Send(actor, $"Equipment:");
        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                MessageSystem.Send(actor, $"{slot}: {item.DisplayName}");
            else
                MessageSystem.Send(actor, $"{slot}: nothing");
        }
        MessageSystem.Send(actor, $"Active tags: {characterEffects.ActiveTags}");
        MessageSystem.Send(actor, $"Effects:");
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
                MessageSystem.Send(actor, $"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining ticks: {remainingTicks}");
            }
            else
                MessageSystem.Send(actor, $"- {effectName} Source: {sourceName} Stacks: {stackCount} Permanent");
            if (hasStatModifiers)
            {
                foreach (var modifier in statModifiers.Values)
                    MessageSystem.Send(actor, $"  - {modifier.Type} {modifier.Value} {modifier.Stat}");
            }
            if (hasDamageOverTime)
                MessageSystem.Send(actor, $"  - Damage over time: {damageOverTime.Damage} every {damageOverTime.TickRate} ticks");
            if (hasHealOverTime)
                MessageSystem.Send(actor, $"  - Heal over time: {healOverTime.Heal} every {healOverTime.TickRate} ticks");
        }
    }
}
