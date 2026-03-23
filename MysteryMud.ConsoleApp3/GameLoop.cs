using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3;

internal class GameLoop
{
    private readonly World _world;

    public GameLoop(World world)
    {
        _world = world;
    }

    public void Run()
    {
        while (true)
        {
            CheckConsoleInput();

            Tick();

            Thread.Sleep(100); // tick rate
        }
    }

    private void Tick()
    {
        TimeSystem.NextTick();

        var state = new GameState
        {
            World = _world,
            CurrentTick = TimeSystem.CurrentTick
        };

        // process player commands
        CommandBus.Process(state);

        // process scheduled events
        Scheduler.Process(state);
        // handle state transitions for effects
        //StateMachineSystem.Update(world); TODO: implement state machine system and handle with scheduled events

        // AiSystem.Process(world);
        // handle combat rounds
        CombatSystem.Process(state);

        // handle deaths and related consequences
        DeathSystem.Process(state);

        // handle player deaths and respawns
        RespawnSystem.Process(state);

        // recalculate stats for entities
        StatSystem.Process(state);

        // perform cleanup tasks like removing characters, items, ...
        CleanupSystem.Process(state);

        // process messages to be sent to players
        MessageBus.Process(state);

        // send output to players
        FlushOutputSystem.Process(state);
    }

    private void CheckConsoleInput()
    {
        if (Console.KeyAvailable)
        {
            var line = Console.ReadLine();
            if (line != null)
            {
                switch (line)
                {
                    case "dump": DumpWorld(); break;
                        //TODO: case "shutdown": Shutdown(); break;
                        //world.Dispose();             // Clearing the world like God in the First Testament
                        //World.Destroy(world);        // Doomsday
                }
            }
        }
    }

    private void DumpWorld()
    {
        Console.WriteLine("Dumping world state:");
        var query = new QueryDescription();
        _world.Query(query, (Entity entity) =>
        {
            Console.WriteLine($"Entity Id: {entity.Id} Alive: {entity.IsAlive()} DebugName: {entity.DebugName}");
            Console.WriteLine($"  Components: {string.Join(", ", entity.GetAllComponents().Select(c => c?.GetType().Name))}");
        });
    }
}
