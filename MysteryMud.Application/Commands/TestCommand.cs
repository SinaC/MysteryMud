using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Factories;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class TestCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.TargetAndText;
    public CommandDefinition Definition { get; }

    public TestCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, CommandContext ctx)
    {
        ref var roomContents = ref actor.Get<Location>().Room.Get<RoomContents>().Characters;
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Primary, roomContents);

        systemContext.Msg.To(actor).Send("Ansi16: %RR%GG%YY%BB%MM%CC%WW%rr%gg%yy%bb%mm%cc%ww%xnocolor");
        systemContext.Msg.To(actor).Send("Ansi256: %=214orange%xnocolor");
        systemContext.Msg.To(actor).Send("RGB: %#FFA500orange%xnocolor");
        systemContext.Msg.To(actor).Send("GRADIENT: %#FFA500>#00FFA5orange-2-cyan%xnocolor");

        if (target == default)
        {
            systemContext.Msg.To(actor).Send("You don't see that here.");
            return;
        }

        if (ctx.Text.Equals("poison".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            var effectTemplate = new EffectTemplate
            {
                Name = "Poison2",
                Tag = EffectTagId.Poison,
                Stacking = StackingRules.Stack,
                MaxStacks = 3,
                StatModifiers =
                [
                    new StatModifierDefinition
                    {
                        Stat = StatTypes.Strength,
                        Value = -2,
                        Type = ModifierTypes.Flat
                    },
                    new StatModifierDefinition
                    {
                        Stat = StatTypes.HitRoll,
                        Value = -3,
                        Type = ModifierTypes.Flat
                    },
                ],
                Flags = AffectFlags.Poison,
                DurationFunc = (world, source, target) => 5,
                Dot = new DotDefinition
                {
                    DamageFunc = (world, source, target) => 45,
                    TickRate = 2,
                    DamageType = DamageTypes.Poison
                },
                Hot = null // not hot
            };

            EffectFactory.ApplyEffect(systemContext, state, effectTemplate, actor, target);
        }
        else if (ctx.Text.Equals("bless".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            var effectTemplate = new EffectTemplate
            {
                Name = "Bless2",
                Tag = EffectTagId.Bless,
                Stacking = StackingRules.Replace,
                MaxStacks = 1,
                StatModifiers =
                [
                    new StatModifierDefinition
                    {
                        Stat = StatTypes.Intelligence,
                        Value = 2,
                        Type = ModifierTypes.Flat
                    },
                    new StatModifierDefinition
                    {
                        Stat = StatTypes.HitRoll,
                        Value = 10,
                        Type = ModifierTypes.AddPercent
                    },
                ],
                Flags = AffectFlags.Bless,
                DurationFunc = (world, source, target) => 60,
                Dot = null, // no dot
                Hot = new HotDefinition
                {
                    HealFunc = (world, source, target) => 5,
                    TickRate = 2
                },
            };

            EffectFactory.ApplyEffect(systemContext, state, effectTemplate, actor, target);
        }
    }
}
