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

        // AiSystem.Process(world);
        // handle combat rounds
        CombatSystem.Process(world);

        // apply damage over time and healing over time effects
        //--handle with events HotSystem.Update(world);
        //--handle with events DotSystem.Update(world);
        // handle state transitions for effects
        //StateMachineSystem.Update(world); TODO: implement state machine system
        // update durations for effects and cooldowns
        //--handle with events rationSystem.Update(world);
        // recalculate stats for entities
        StatSystem.Recalculate(world);

        // handle player deaths and respawns
        RespawnSystem.RespawnPlayers(world);

        // perform cleanup tasks like removing characters, items, ...
        CleanupSystem.Cleanup(world);

        // send output to players
        FlushOutpusSystem.FlushOutputs(world);
    }
}
