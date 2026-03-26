using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Scheduler;
using MysteryMud.Core.Services;
using MysteryMud.Domain;
using MysteryMud.Domain.Systems;
using MysteryMud.Infrastructure.Services;

namespace MysteryMud.ConsoleApp.Hosting;

internal class GameLoop
{
    private readonly ILogger _logger;
    private readonly IMessageService _messageService;
    private readonly ICommandBus _commandBus;
    private readonly IMessageBus _messageBus;
    private readonly IScheduler _scheduler;
    private readonly IActService _actService;
    private readonly World _world;

    public GameLoop(ILogger logger, IMessageService messageService, ICommandBus commandBus, IMessageBus messageBus, IScheduler scheduler, IActService actService, World world)
    {
        _logger = logger;
        _messageService = messageService;
        _commandBus = commandBus;
        _messageBus = messageBus;
        _scheduler = scheduler;
        this._actService = actService;
        _world = world;
    }

    public void Run()
    {
        _logger.LogInformation(LogEvents.System, "Starting game loop");

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
            Log = _logger,
            Msg = _messageBus,
            Scheduler = _scheduler,
            Act = _actService
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
        CleanupSystem.Process(systemContext, state);

        // process messages to be sent to players
        _messageBus.Process(systemContext, state);

        // send output to players
        _messageService.FlushAll();
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