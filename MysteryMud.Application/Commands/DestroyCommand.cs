using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class DestroyCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.Target;
    public CommandDefinition Definition { get; }

    public DestroyCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Destroy what ?");
            return;
        }

        // search in inventory (equipped items are also in inventory)
        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // intent to destroy item
            ref var destroyItemIntent = ref systemContext.Intent.DestroyItem.Add();
            destroyItemIntent.Entity = actor;
            destroyItemIntent.Item = item;
        }
    }
}
