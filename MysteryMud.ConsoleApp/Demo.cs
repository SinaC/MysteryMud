using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Commands;
using MysteryMud.Application.Commands.Dispatcher;
using MysteryMud.Application.Commands.Registry;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Domain;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Infrastructure.Scheduler;

namespace MysteryMud.ConsoleApp;

static class Demo
{
    public static void Run(ILogger logger, World world)
    {
        var commandRegistry = new CommandRegistry();
        var commandDispatcher = new CommandDispatcher(commandRegistry);

        // get entities for testing
        Span<Entity> characters = stackalloc Entity[10];
        world.GetEntities(new QueryDescription().WithAll<Health>(), characters);
        var player = characters.ToArray().First(x => x.Get<Name>().Value == "player");
        var goblin = characters.ToArray().First(x => x.Get<Name>().Value == "goblin");

        Span<Entity> rooms = stackalloc Entity[10];
        world.GetEntities(new QueryDescription().WithAll<Room>(), rooms);
        var temple = rooms.ToArray().First(x => x.Get<Name>().Value == "temple square");

        Span<Entity> items = stackalloc Entity[10];
        world.GetEntities(new QueryDescription().WithAll<Item>(), items);
        var chest = items.ToArray().First(x => x.Get<Name>().Value == "chest");
        var gem = items.ToArray().First(x => x.Get<Name>().Value == "gem");

        // game state for testing
        var gameState = new GameState { World = world, CurrentTick = 0 };
        // system context for testing
        var systemContext = new SystemContext { Log = logger, MessageBus = new DemoMessageBus(), Scheduler = new Scheduler() };

        // add commands
        // register commands
        commandRegistry.RegisterCommand("test", new TestCommand());
        commandRegistry.RegisterCommand("kill", new KillCommand());
        commandRegistry.RegisterCommand("get", new GetCommand());
        commandRegistry.RegisterCommand("look", new LookCommand());
        commandRegistry.RegisterCommand("say", new SayCommand());
        commandRegistry.RegisterCommand("tell", new TellCommand());
        commandRegistry.RegisterCommand("inventory", new InventoryCommand());
        commandRegistry.RegisterCommand("equipment", new EquipmentCommand());
        commandRegistry.RegisterCommand("wear", new WearCommand());
        commandRegistry.RegisterCommand("remove", new RemoveCommand());
        commandRegistry.RegisterCommand("drop", new DropCommand());
        commandRegistry.RegisterCommand("give", new GiveCommand());
        commandRegistry.RegisterCommand("put", new PutCommand());
        commandRegistry.RegisterCommand("north", new NorthCommand());
        commandRegistry.RegisterCommand("south", new SouthCommand());
        commandRegistry.RegisterCommand("destroy", new DestroyCommand());
        commandRegistry.RegisterCommand("sacrifice", new SacrificeCommand());
        commandRegistry.RegisterCommand("mstat", new MstatCommand());
        commandRegistry.RegisterCommand("cast", new CastCommand());

        // test commands
        commandDispatcher.Dispatch(systemContext, gameState, player, "look".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "get all.sword".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "look".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "inventory".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, goblin, "say you stole my sword".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "tell goblin you're a liar".AsSpan());
        //commandDispatcher.Dispatch(systemContext, gameState, sword, "inventory".AsSpan()); will crash because sword is not a character and doesn't have inventory component
        commandDispatcher.Dispatch(systemContext, gameState, player, "look chest".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "get all from chest".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "inventory".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "wear gem".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "wear sword".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "inventory".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "equipment".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "drop toto".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "drop gem".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "give sword goblin".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, goblin, "wear sword".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, goblin, "remove sword".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, goblin, "wear sword".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "look goblin".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "inventory".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "get gem".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "put gem chest".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "look chest".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, player, "look".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, temple, "look".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, chest, "look".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, gem, "look".AsSpan());
        commandDispatcher.Dispatch(systemContext, gameState, goblin, "get trash".AsSpan());

        ////testing combat
        //commandDispatcher.Dispatch(systemContext, gameState, player, "kill goblin".AsSpan());

        ////testing buffs and dots
        //commandDispatcher.Dispatch(systemContext, gameState, goblin, "test troll poison".AsSpan());
        //commandDispatcher.Dispatch(systemContext, gameState, player, "test troll poison".AsSpan()); // will apply a second stack of poison because StackingRule is Stack
        //commandDispatcher.Dispatch(systemContext, gameState, player, "test troll poison".AsSpan()); // will apply a second stack of poison because StackingRule is Stack
        //commandDispatcher.Dispatch(systemContext, gameState, player, "test troll bless".AsSpan()); // will not be applied because StackingRule is None
        //commandDispatcher.Dispatch(systemContext, gameState, player, "test troll bless".AsSpan());

        //commandDispatcher.Dispatch(systemContext, gameState, goblin, "test troll poison".AsSpan());
        //commandDispatcher.Dispatch(systemContext, gameState, player, "test troll poison".AsSpan());
        //commandDispatcher.Dispatch(systemContext, gameState, goblin, "test troll poison".AsSpan());
    }

    private class DemoMessageBus : IMessageBus
    {
        public void Publish(Entity entity, string message)
        {
            Console.WriteLine($"Message to {entity.DebugName}: {message}");
        }
        public void Process(SystemContext ctx, GameState gameState)
        {
            // no-op for demo
        }
    }
}
