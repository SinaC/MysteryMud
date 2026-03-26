using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components.Characters;
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

        systemContext.Msg.Send(actor, "You are wearing:");

        foreach (var slot in Enum.GetValues<EquipmentSlots>())
        {
            if (equipment.Slots.TryGetValue(slot, out var item))
                systemContext.Msg.Send(actor, $"{slot}: {item.DisplayName}");
            else
                systemContext.Msg.Send(actor, $"{slot}: nothing");
        }
    }
}
