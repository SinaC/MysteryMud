using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Domain.Factories;
using MysteryMud.Domain.Systems;

namespace MysteryMud.Application.Commands;

public class TestCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.TargetAndText;
    public CommandDefinition Definition { get; }

    public TestCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        ref var roomContents = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = TargetingSystem.SelectSingleTarget(actor, ctx.Primary, roomContents);

        if (target == default)
        {
            systemContext.MessageBus.Publish(actor, "You don't see that here.");
            return;
        }

        if (ctx.Text.Equals("poison".AsSpan(), StringComparison.OrdinalIgnoreCase))
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
                    DamageFunc = (world, source, target) => 45,
                    TickRate = 2,
                    DamageType = DamageType.Poison
                },
                Hot = null // not hot
            };

            EffectFactory.ApplyEffect(systemContext, gameState, effectTemplate, actor, target);
        }
        else if (ctx.Text.Equals("bless".AsSpan(), StringComparison.OrdinalIgnoreCase))
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

            EffectFactory.ApplyEffect(systemContext, gameState, effectTemplate, actor, target);
        }
    }
}
