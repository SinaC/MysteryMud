using MysteryMud.Core;
using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class AISystem
{
    private World _world;

    public AISystem(World world)
    {
        _world = world;
    }

    private static readonly QueryDescription _autoCommandQueryDesc = new QueryDescription()
        .WithAll<CommandBuffer, AutoCommand>();

    public void Execute(GameState state)
    {
        long now = state.CurrentTimeMs;

        _world.Query(_autoCommandQueryDesc, (EntityId entity,
            ref CommandBuffer buffer,
            ref AutoCommand autoCommand) =>
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
            if (!_world.Has<HasCommandTag>(entity))
                _world.Add<HasCommandTag>(entity);

            // Update last AI tick
            autoCommand.LastCommandTick = now;
        });
    }

    private IEnumerable<CommandRequest> DecideNextCommands(EntityId npc, AutoCommand autoCommand)
    {
        // TODO
        //// Example: move or attack
        //yield return new CommandRequest { CommandId = 1, RawCommand = "move", RawArgs = "north" };
        //yield return new CommandRequest { CommandId = 2, RawCommand = "kill", RawArgs= "player" };
        yield break;
    }
}
