using Arch.Core;
using MysteryMud.ConsoleApp3.Events;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3;

static class GameLoop
{
    public static void Tick(World world)
    {
        TimeSystem.NextTick();

        //Console.WriteLine($"Tick: {TimeSystem.CurrentTick}");

        // process player commands
        CommandSystem.ProcessCommands(world);

        // process scheduled events
        EventScheduler.ProcessEvents(world, TimeSystem.CurrentTick);
        // handle state transitions for effects
        //StateMachineSystem.Update(world); TODO: implement state machine system and handle with scheduled events

        // AiSystem.Process(world);
        // handle combat rounds
        CombatSystem.Process(world);

        // handle deaths and related consequences
        DeathSystem.Process(world);

        // handle player deaths and respawns
        RespawnSystem.RespawnPlayers(world);

        // recalculate stats for entities
        StatSystem.Recalculate(world);

        // perform cleanup tasks like removing characters, items, ...
        CleanupSystem.Cleanup(world);

        // send output to players
        FlushOutpusSystem.FlushOutputs(world);
    }
}
