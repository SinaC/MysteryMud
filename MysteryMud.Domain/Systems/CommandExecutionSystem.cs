using DefaultEcs;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Extensions;

namespace MysteryMud.Domain.Systems;

public class CommandExecutionSystem
{
    private readonly ILogger _logger;
    private readonly EntitySet _hasCommandEntitySet;

    public CommandExecutionSystem(World world, ILogger logger)
    {
        _logger = logger;
        _hasCommandEntitySet = world
            .GetEntities()
            .With<CommandBuffer>()
            .With<HasCommandTag>()
            .AsSet();
    }

    public void Execute(GameState state)
    {
        long now = state.CurrentTimeMs;

        foreach (var entity in _hasCommandEntitySet.GetEntities())
        {
            ref var buffer = ref entity.Get<CommandBuffer>();

            if (!entity.IsAlive)
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
                    _logger.LogError(ex, "Error executing command {Command} for {Entity}", request.CommandSpan.ToString(), entity.DebugName);
                }
            }

            // clear buffer after execution
            buffer.Clear();
            // remove has active command tag
            entity.Remove<HasCommandTag>();
        }
    }
}