using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data;
using MysteryMud.ConsoleApp3.Data.Enums;
using MysteryMud.ConsoleApp3.Factories;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class TestCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetAndText;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        var roomContents = actor.Get<Position>().Room.Get<RoomContents>().Characters;
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Primary, roomContents);

        if (target == default)
        {
            MessageSystem.Send(actor, "You don't see that here.");
            return;
        }

        if (MemoryExtensions.Equals(ctx.Text, "poison", StringComparison.OrdinalIgnoreCase))
        {
            var effectTemplate = new EffectTemplate
            {
                Name = "Poison",
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
                DurationFunc = (world, source, target) => 100,
                Dot = new DotDefinition
                {
                    DamageFunc = (world, source, target) => 3,
                    TickRate = 2,
                    DamageType = DamageType.Poison
                },
                Hot = null // not hot
            };

            EffectFactory.ApplyEffect(world, effectTemplate, actor, target);
        }
        else if (MemoryExtensions.Equals(ctx.Text, "bless", StringComparison.OrdinalIgnoreCase))
        {
            var effectTemplate = new EffectTemplate
            {
                Name = "Bless",
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
                DurationFunc = default, // no duration
                Dot = null, // no dot
                Hot = new HotDefinition
                {
                    HealFunc = (world, source, target) => 5,
                    TickRate = 2
                },
            };

            EffectFactory.ApplyEffect(world, effectTemplate, actor, target);
        }
    }
}
