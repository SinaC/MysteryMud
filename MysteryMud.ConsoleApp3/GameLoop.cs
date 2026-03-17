using Arch.Core;
using MysteryMud.ConsoleApp3.Commands;
using MysteryMud.ConsoleApp3.Components.Networking;
using MysteryMud.ConsoleApp3.Systems;
using System.Buffers;

namespace MysteryMud.ConsoleApp3;

class GameLoop
{
    //EventScheduler scheduler;

    public void Tick(World world)
    {
        //scheduler.Update(time);

        TimeSystem.NextTick();

        ProcessCommands(world);

        // AiSystem.Process(world);

        DurationSystem.Update(world);
        HotSystem.Update(world);
        DotSystem.Update(world);
        StatSystem.Recalculate(world);

        CombatSystem.Process(world);

        RespawnSystem.RespawnPlayers(world);

        CleanupSystem.Cleanup(world);

        FlushOutputs(world);
    }

    void ProcessCommands(World world)
    {
        while (CommandQueue.TryDequeue(out var cmd))
        {
            var span = cmd.Buffer.AsSpan(0, cmd.Length);

            // TODO: check min position
            CommandDispatcher.Dispatch(world, cmd.Player, span);

            ArrayPool<char>.Shared.Return(cmd.Buffer);
        }
    }

    void FlushOutputs(World world)
    {
        var query = new QueryDescription()
                .WithAll<Connection>();
        world.Query(query, (Entity player,
                     ref Connection conn) =>
        {
            conn.Value.Flush();  // signals SendLoop to send batched output
        });
    }
}
