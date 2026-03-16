using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data;
using MysteryMud.ConsoleApp3.Data.EffectTemplates;
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
            var poisonSpellDefinition = new SpellDefinition
            {
                Id = 1,
                Name = "Poison",
                Duration = 5,
                ApplyMessage = "You have been poisoned!",
                WearOffMessage = "The poison wears off.",
                EffectTemplates =
                [
                    new StatModifierTemplate
                    {
                        Modifiers =
                        [
                            new StatModifier
                            {
                                Stat = StatType.Strength,
                                Value = -2,
                                Type = ModifierType.Flat
                            },
                            new StatModifier
                            {
                                Stat = StatType.HitRoll,
                                Value = -2,
                                Type = ModifierType.AddPercent
                            },
                        ],
                    },
                    new DotTemplate
                    {
                        Damage = 2,
                        TickRate = 2,
                        DamageType = DamageType.Poison
                    }
                ]
            };

            EffectFactory.ApplySpell(world, poisonSpellDefinition, actor, target);
        }
        else if (MemoryExtensions.Equals(ctx.Text, "bless", StringComparison.OrdinalIgnoreCase))
        {
            var blessSpellDefinition = new SpellDefinition
            {
                Id = 1,
                Name = "Bless",
                Duration = 50,
                ApplyMessage = "You are blessed!",
                WearOffMessage = "You feel less blessed.",
                EffectTemplates =
                [
                    new StatModifierTemplate
                    {
                        Modifiers =
                        [
                            new StatModifier
                            {
                                Stat = StatType.Strength,
                                Value = 2,
                                Type = ModifierType.Flat
                            },
                            new StatModifier
                            {
                                Stat = StatType.HitRoll,
                                Value = 1,
                                Type = ModifierType.AddPercent
                            },
                        ],
                    }
                ]
            };

            EffectFactory.ApplySpell(world, blessSpellDefinition, actor, target);
        }
    }
}
