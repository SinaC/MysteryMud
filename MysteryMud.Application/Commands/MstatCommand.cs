using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class MstatCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.Target;
    public CommandDefinition Definition { get; }

    public MstatCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Mstat what ?");
            return;
        }

        var people = actor.Get<Location>().Room.Get<RoomContents>().Characters;

        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Primary, people);
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
        foreach (var stat in Enum.GetValues<StatTypes>())
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
        foreach (var slot in Enum.GetValues<EquipmentSlots>())
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
                var remainingTicks = duration.ExpirationTick - gameState.CurrentTick;
                systemContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining ticks: {remainingTicks}");
            }
            else
                systemContext.Msg.To(actor).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Permanent");
            if (hasStatModifiers)
            {
                foreach (var modifier in statModifiers.Values)
                    systemContext.Msg.To(actor).Send($"  - {modifier.Type} {modifier.Value} {modifier.Stat}");
            }
            if (hasDamageOverTime)
                systemContext.Msg.To(actor).Send($"  - Damage over time: {damageOverTime.Damage} every {damageOverTime.TickRate} ticks");
            if (hasHealOverTime)
                systemContext.Msg.To(actor).Send($"  - Heal over time: {healOverTime.Heal} every {healOverTime.TickRate} ticks");
        }
    }
}
