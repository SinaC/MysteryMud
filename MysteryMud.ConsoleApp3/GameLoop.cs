using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Components.Extensions;
using MysteryMud.ConsoleApp3.Infrastructure.Services;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3;

internal class GameLoop
{
    private readonly IMessageService _messageService;
    private readonly ICommandBus _commandBus;
    private readonly IMessageBus _messageBus;
    private readonly IScheduler _scheduler;
    private readonly World _world;

    public GameLoop(IMessageService messageService, ICommandBus commandBus, IMessageBus messageBus, IScheduler scheduler, World world)
    {
        _messageService = messageService;
        _commandBus = commandBus;
        _messageBus = messageBus;
        _scheduler = scheduler;
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

        var systemContext = new SystemContext
        {
            CommandBus = _commandBus,
            MessageBus = _messageBus,
            Scheduler = _scheduler
        };


        // process player commands
        _commandBus.Process(systemContext, state);

        // process scheduled events
        _scheduler.Process(systemContext, state);
        // handle state transitions for effects
        //StateMachineSystem.Update(world); TODO: implement state machine system and handle with scheduled events

        // AiSystem.Process(world);
        // handle combat rounds
        CombatSystem.Process(systemContext, state);

        // handle deaths and related consequences
        DeathSystem.Process(systemContext, state);

        // handle player deaths and respawns
        RespawnSystem.Process(systemContext, state);

        // recalculate stats for entities
        StatSystem.Process(state);

        // perform cleanup tasks like removing characters, items, ...
        CleanupSystem.Process(state);

        // process messages to be sent to players
        _messageBus.Process(systemContext, state);

        // send output to players
        FlushOutputSystem.Process(_messageService, state);
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
