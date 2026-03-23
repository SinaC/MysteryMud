using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Data.Definitions;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class TestCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetAndText;

    public void Execute(GameState gameState, Entity actor, CommandContext ctx)
    {
        ref var roomContents = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Primary, roomContents);

        if (target == default)
        {
            MessageBus.Publish(actor, "You don't see that here.");
            return;
        }

        if (MemoryExtensions.Equals(ctx.Text, "poison", StringComparison.OrdinalIgnoreCase))
        {
            var effectTemplate = new EffectTemplate
            {
                Name = "Poison2",
                Tag = EffectTagId.Poison,
                Stacking = StackingRule.Stack,
                MaxStacks = 3,
                StatModifiers =
                [
                    new StatModifierDefinition
                    {
                        Stat = StatType.Strength,
                        Value = -2,
                        Type = ModifierType.Flat
                    },
                    new StatModifierDefinition
                    {
                        Stat = StatType.HitRoll,
                        Value = -3,
                        Type = ModifierType.Flat
                    },
                ],
                Flags = AffectFlags.Poison,
                DurationFunc = (world, source, target) => 5,
                Dot = new DotDefinition
                {
                    DamageFunc = (world, source, target) => 3,
                    TickRate = 2,
                    DamageType = DamageType.Poison
                },
                Hot = null // not hot
            };

            EffectFactory.ApplyEffect(gameState, effectTemplate, actor, target);
        }
        else if (MemoryExtensions.Equals(ctx.Text, "bless", StringComparison.OrdinalIgnoreCase))
        {
            var effectTemplate = new EffectTemplate
            {
                Name = "Bless2",
                Tag = EffectTagId.Bless,
                Stacking = StackingRule.None,
                MaxStacks = 1,
                StatModifiers =
                [
                    new StatModifierDefinition
                    {
                        Stat = StatType.Intelligence,
                        Value = 2,
                        Type = ModifierType.Flat
                    },
                    new StatModifierDefinition
                    {
                        Stat = StatType.HitRoll,
                        Value = 10,
                        Type = ModifierType.AddPercent
                    },
                ],
                Flags = AffectFlags.Bless,
                DurationFunc = (world, source, target) => 30,
                Dot = null, // no dot
                Hot = new HotDefinition
                {
                    HealFunc = (world, source, target) => 5,
                    TickRate = 2
                },
            };

            EffectFactory.ApplyEffect(gameState, effectTemplate, actor, target);
        }
    }
}
