using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Factories;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.ExplicitCommands;

public class TestCommand : ICommand
{
    private const string Name = "test";

    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetAndText;

    public CommandDefinition Definition { get; }

    public TestCommand()
    {
        Definition = new CommandDefinition
        {
            Id = Name.ComputeCommandId(),
            Name = Name,
            Aliases = [],
            CannotBeForced = true,
            RequiredLevel = CommandLevelKind.Admin,
            MinimumPosition = PositionKind.Dead,
            Priority = 0,
            AllowAbbreviation = false,
            HelpText = "",
            Syntaxes = ["[cmd]"],
            Categories = ["test"],
            ThrottlingCategories = CommandThrottlingCategories.Admin
        };
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        ref var roomContents = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary, roomContents);

        //systemContext.Msg.To(actor).Send("Ansi16: %RR%GG%YY%BB%MM%CC%WW%rr%gg%yy%bb%mm%cc%ww%xnocolor");
        //systemContext.Msg.To(actor).Send("Ansi256: %=214orange%xnocolor");
        //systemContext.Msg.To(actor).Send("RGB: %#FFA500orange%xnocolor");
        //systemContext.Msg.To(actor).Send("GRADIENT: %#FFA500>#00FFA5orange-2-cyan%xnocolor");

        if (target == default)
        {
            systemContext.Msg.To(actor).Send("You don't see that here.");
            return;
        }

        if (ctx.Text.Equals("poison".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            var effectDefinition = new EffectDefinition
            {
                Id = "Poison2",
                Tag = EffectTagId.Poison,
                Stacking = StackingRule.Stack,
                MaxStacks = 3,
                StatModifiers =
                [
                    new StatModifierDefinition
                    {
                        Stat = StatKind.Strength,
                        Value = -2,
                        Kind = ModifierKind.Flat
                    },
                    new StatModifierDefinition
                    {
                        Stat = StatKind.HitRoll,
                        Value = -3,
                        Kind = ModifierKind.Flat
                    },
                ],
                Flags = AffectFlags.Poison,
                DurationFunc = (world, source, target) => 15,
                TickRate = 2,
                Dot = new DotDefinition
                {
                    DamageFunc = (world, source, target) => 45,
                    DamageKind = DamageKind.Poison
                },
                Hot = null // not hot
            };

            //TODO: EffectFactory.ApplyEffect(systemContext, state, effectDefinition, actor, target);
        }
        else if (ctx.Text.Equals("poison2".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            var effectDefinition = new EffectDefinition
            {
                Id = "Poison3",
                Tag = EffectTagId.Poison,
                Stacking = StackingRule.Refresh,
                MaxStacks = 3,
                StatModifiers = [],
                Flags = AffectFlags.Poison,
                DurationFunc = (world, source, target) => 60,
                TickRate = 2,
                Dot = new DotDefinition
                {
                    DamageFunc = (world, source, target) => 5,
                    DamageKind = DamageKind.Poison
                },
                Hot = null // not hot
            };

            //TODO: EffectFactory.ApplyEffect(systemContext, state, effectDefinition, actor, target);
        }
        else if (ctx.Text.Equals("bless".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            var effectDefinition = new EffectDefinition
            {
                Id = "Bless2",
                Tag = EffectTagId.Bless,
                Stacking = StackingRule.Replace,
                MaxStacks = 1,
                StatModifiers =
                [
                    new StatModifierDefinition
                    {
                        Stat = StatKind.Intelligence,
                        Value = 2,
                        Kind = ModifierKind.Flat
                    },
                    new StatModifierDefinition
                    {
                        Stat = StatKind.HitRoll,
                        Value = 10,
                        Kind = ModifierKind.AddPercent
                    },
                ],
                Flags = AffectFlags.Bless,
                DurationFunc = (world, source, target) => 60,
                TickRate = 2,
                Dot = null, // no dot
                Hot = new HotDefinition
                {
                    HealFunc = (world, source, target) => 5,
                },
            };

            //TODO: EffectFactory.ApplyEffect(systemContext, state, effectDefinition, actor, target);
        }
    }
}
