using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Domain.Systems;

public class CommandExecutionSystem
{
    //private const int MaxEntitiesPerTick = 100;

    public void Execute(SystemContext systemContext, GameState state)
    {
        //var processed = 0;

        var query = new QueryDescription()
            .WithAll<CommandBuffer, HasCommandTag>();
        state.World.Query(query, (Entity entity, ref CommandBuffer buffer, ref HasCommandTag hasCommandTag) =>
        {
            if (!entity.IsAlive())
                return;

            for (int i = 0; i < buffer.Count; i++)
            {
                ref var request = ref buffer.Items[i];

                if (request.Cancelled)
                    continue;

                //var command = _registry.Get(request.CommandId);

                var cmd = request.RawCommand.AsSpan();
                var args = request.RawArgs.AsSpan();

                // TODO: should be commandId and use CommandRegistry to get the command
                // execute command
                request.Command.Execute(
                    systemContext,
                    state,
                    entity,
                    cmd,
                    args
                );
            }

            // clear buffer after execution
            buffer.Count = 0;
            // remove tag
            entity.Remove<HasCommandTag>();

            // TODO
            //processed++;
            //if (processed >= MaxEntitiesPerTick)
            //    break; // defer remaining entities to next tick
        });
    }
}
