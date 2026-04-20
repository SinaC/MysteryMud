using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;

namespace MysteryMud.Domain.Systems;

public class AISystem
{
    public void Execute(GameState state)
    {
        long now = state.CurrentTimeMs;

        var query = new QueryDescription()
            .WithAll<CommandBuffer, AutoCommand>();
        state.World.Query(query, (Entity entity, ref CommandBuffer buffer, ref AutoCommand autoCommand) =>
        {
            // Skip if AI tick rate not reached
            if (now - autoCommand.LastCommandTick < autoCommand.CommandTickRate)
                return;

            // Decide AI commands for this NPC
            var commands = DecideNextCommands(entity, autoCommand);

            // Push commands into NPC buffer
            foreach (var cmd in commands)
            {
                if (buffer.Items == null)
                    buffer.Items = new CommandRequest[4]; // small start size

                if (buffer.Count == buffer.Items.Length)
                    Array.Resize(ref buffer.Items, buffer.Items.Length * 2);

                buffer.Items[buffer.Count++] = cmd;
            }

            // Mark entity for processing
            if (!entity.Has<HasCommandTag>())
                entity.Add<HasCommandTag>();

            // Update last AI tick
            autoCommand.LastCommandTick = now;
        });
    }

    private IEnumerable<CommandRequest> DecideNextCommands(Entity npc, AutoCommand autoCommand)
    {
        // TODO
        //// Example: move or attack
        //yield return new CommandRequest { CommandId = 1, RawCommand = "move", RawArgs = "north" };
        //yield return new CommandRequest { CommandId = 2, RawCommand = "kill", RawArgs= "player" };
        yield break;
    }
}
