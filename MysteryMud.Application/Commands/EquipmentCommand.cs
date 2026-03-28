using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class EquipmentCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.None;
    public CommandDefinition Definition { get; }

    public EquipmentCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        ref var equipment = ref actor.Get<Equipment>();

        systemContext.Msg.To(actor).Send("You are wearing:");

        foreach (var slot in Enum.GetValues<EquipmentSlots>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                systemContext.Msg.To(actor).Send($"{slot}: {item.DisplayName}");
            else
                systemContext.Msg.To(actor).Send($"{slot}: nothing");
        }
    }
}
