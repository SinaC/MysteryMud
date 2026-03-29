using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class FleeCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.None;

    public CommandDefinition Definition { get; }

    public FleeCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (!actor.Has<CombatState>())
        {
            systemContext.Msg.To(actor).Send("You aren't fighting anyone.");
            return;
        }

        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // intent to flee
        ref var intent = ref systemContext.Intent.Flee.Add();
        intent.Entity = actor;
        intent.FromRoom = room;
    }
}
