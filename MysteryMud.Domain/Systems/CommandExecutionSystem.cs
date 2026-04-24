using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Helpers;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class CommandExecutionSystem
{
    private readonly World _world;
    private readonly ILogger _logger;

    public CommandExecutionSystem(World world, ILogger logger)
    {
        _world = world;
        _logger = logger;
    }

    private static readonly QueryDescription _hasCommandQueryDesc = new QueryDescription()
        .WithAll<CommandBuffer, HasCommandTag>();

    public void Execute(GameState state)
    {
        long now = state.CurrentTimeMs;

        _world.Query(_hasCommandQueryDesc, (EntityId entity,
            ref CommandBuffer buffer,
            ref HasCommandTag _) =>
        {
            if (!CharacterHelpers.IsAlive(_world, entity))
                return;

            int writeIndex = 0; // keep commands not ready yet

            for (int i = 0; i < buffer.Count; i++)
            {
                ref var request = ref buffer.Items[i];

                // Skip cancelled commands
                if (request.Cancelled)
                    continue;

                // Only execute if throttling allows
                if (request.ExecuteAt > now)
                {
                    // keep for next tick
                    buffer.Items[writeIndex++] = request;
                    continue;
                }

                try
                {
                    request.Command.Handler.Execute(state, entity, request.CommandSpan, request.ArgsSpan);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing command {Command} for {Entity}", request.CommandSpan.ToString(), EntityHelpers.DebugName(_world, entity));
                }
            }

            // clear buffer after execution
            buffer.Clear();
            // remove has active command tag
            _world.Remove<HasCommandTag>(entity);
        });
    }
}