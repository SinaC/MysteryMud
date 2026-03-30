using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class GiveCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.TargetPair;
    public CommandDefinition Definition { get; }

    public GiveCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount < 2)
        {
            systemContext.Msg.To(actor).Send("Give what to whom ?");
            return;
        }

        ref var inventory = ref actor.Get<Inventory>();
        ref var room = ref actor.Get<Location>().Room;
        ref var roomContents = ref room.Get<RoomContents>();

        // Find target character in room
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Secondary, roomContents.Characters);
        if (target == default)
        {
            systemContext.Msg.To(actor).Send("They are not here.");
            return;
        }

        foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // intent to give item
            ref var giveItemIntent = ref systemContext.Intent.GiveItem.Add();
            giveItemIntent.Entity = actor;
            giveItemIntent.Item = item;
            giveItemIntent.Target = target;
        }
    }
}
