using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Commands.Dispatcher;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Scheduler;
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
        var systemContext = new SystemContext { Log = logger, CommandBus = null!, MessageBus = new DemoMessageBus(), Scheduler = new Scheduler() };

        // test commands
        CommandDispatcher.Dispatch(systemContext, gameState, player, "look".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "get all.sword".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "look".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "inventory".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, goblin, "say you stole my sword".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "tell goblin you're a liar".AsSpan());
        //CommandDispatcher.Dispatch(systemContext, gameState, sword, "inventory".AsSpan()); will crash because sword is not a character and doesn't have inventory component
        CommandDispatcher.Dispatch(systemContext, gameState, player, "look chest".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "get all from chest".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "inventory".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "wear gem".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "wear sword".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "inventory".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "equipment".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "drop toto".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "drop gem".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "give sword goblin".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, goblin, "wear sword".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, goblin, "remove sword".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, goblin, "wear sword".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "look goblin".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "inventory".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "get gem".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "put gem chest".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "look chest".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, player, "look".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, temple, "look".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, chest, "look".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, gem, "look".AsSpan());
        CommandDispatcher.Dispatch(systemContext, gameState, goblin, "get trash".AsSpan());

        ////testing combat
        //CommandDispatcher.Dispatch(systemContext, gameState, player, "kill goblin".AsSpan());

        ////testing buffs and dots
        //CommandDispatcher.Dispatch(systemContext, gameState, goblin, "test troll poison".AsSpan());
        //CommandDispatcher.Dispatch(systemContext, gameState, player, "test troll poison".AsSpan()); // will apply a second stack of poison because StackingRule is Stack
        //CommandDispatcher.Dispatch(systemContext, gameState, player, "test troll poison".AsSpan()); // will apply a second stack of poison because StackingRule is Stack
        //CommandDispatcher.Dispatch(systemContext, gameState, player, "test troll bless".AsSpan()); // will not be applied because StackingRule is None
        //CommandDispatcher.Dispatch(systemContext, gameState, player, "test troll bless".AsSpan());

        //CommandDispatcher.Dispatch(systemContext, gameState, goblin, "test troll poison".AsSpan());
        //CommandDispatcher.Dispatch(systemContext, gameState, player, "test troll poison".AsSpan());
        //CommandDispatcher.Dispatch(systemContext, gameState, goblin, "test troll poison".AsSpan());
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
