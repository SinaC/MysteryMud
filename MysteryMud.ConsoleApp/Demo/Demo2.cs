using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Application.Dispatching;
using MysteryMud.Application.Services;
using MysteryMud.ConsoleApp.Hosting;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Effects;
using MysteryMud.Core.Eventing;
using MysteryMud.Domain.Action;
using MysteryMud.Domain.Action.Attack.Factories;
using MysteryMud.Domain.Action.Attack.Resolvers;
using MysteryMud.Domain.Action.Damage;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Action.Effect.Factories;
using MysteryMud.Domain.Action.Heal;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.Infrastructure.Eventing;
using MysteryMud.Infrastructure.Intent;
using MysteryMud.Infrastructure.Services;

namespace MysteryMud.ConsoleApp.Demo;

static class Demo2
{
    public static void Run(ILogger logger, World world, ICommandDispatcher commandDispatcher, EffectRegistry effectRegistry)
    {
        // get entities for testing
        Span<Entity> characters = stackalloc Entity[10];
        world.GetEntities(new QueryDescription().WithAll<Health>(), characters);
        var player = characters.ToArray().First(x => x.Get<Name>().Value == "player");
        var goblin = characters.ToArray().First(x => x.Get<Name>().Value == "goblin");
        var troll = characters.ToArray().First(x => x.Get<Name>().Value == "troll");

        Span<Entity> rooms = stackalloc Entity[10];
        world.GetEntities(new QueryDescription().WithAll<Room>(), rooms);
        var temple = rooms.ToArray().First(x => x.Get<Name>().Value == "temple square");

        Span<Entity> items = stackalloc Entity[10];
        world.GetEntities(new QueryDescription().WithAll<ItemTag>(), items);
        var chest = items.ToArray().First(x => x.Get<Name>().Value == "chest");
        var gem = items.ToArray().First(x => x.Get<Name>().Value == "gem");

        // game state for testing
        var gameState = new GameState { World = world, CurrentTick = 0, CurrentTimeMs = 0 };
        // system context for testing
        var messageBus = new DemoMessageBus();
        var actService = new ActService();
        var gameMessageService = new GameMessageService(messageBus, actService);
        var lookService = new LookService(gameMessageService);

        // initialize systems and buffers
        var intentBusContainer = new IntentBusContainer();
        var fleeBlockedEventBuffer = new EventBuffer<FleeBlockedEvent>();
        var movedEventBuffer = new EventBuffer<MovedEvent>();
        var itemGotEventBuffer = new EventBuffer<ItemGotEvent>();
        var itemDroppedEventBuffer = new EventBuffer<ItemDroppedEvent>();
        var itemGivenEventBuffer = new EventBuffer<ItemGivenEvent>();
        var itemPutEventBuffer = new EventBuffer<ItemPutEvent>();
        var itemWornEventBuffer = new EventBuffer<ItemWornEvent>();
        var itemRemovedEventBuffer = new EventBuffer<ItemRemovedEvent>();
        var itemDestroyedEventBuffer = new EventBuffer<ItemDestroyedEvent>();
        var itemSacrifierEventBuffer = new EventBuffer<ItemSacrifiedEvent>();
        var damagedEventBuffer = new EventBuffer<DamagedEvent>();
        var healedEventBuffer = new EventBuffer<HealedEvent>();
        var deathEventBuffer = new EventBuffer<DeathEvent>();
        var itemLootedEventBuffer = new EventBuffer<ItemLootedEvent>();
        var lookedEventBuffer = new EventBuffer<LookedEvent>();
        var attackResolvedEventBuffer = new EventBuffer<AttackResolvedEvent>();
        var effectResolvedEventBuffer = new EventBuffer<EffectResolvedEvent>();

        var executionContext = new CommandExecutionContext { Msg = gameMessageService, Intent = intentBusContainer };

        // TODO: deathEvent and damageEvent are purely combat events and should probably remains the only event passed to systems, other events like itemGotEvent, itemDroppedEvent, itemGivenEvent, itemPutEvent can be directly sent to message service without going through event buffer since they are only used for messaging and no system needs to react to them, this way we can avoid the complexity of managing multiple event buffers and also avoid the issue of events being processed in the wrong order (like damage events being processed before attack intents)
        // TODO: we should replace all these eventbuffers with a more generic event system

        var aggroResolver = new AggroResolver();
        var damageResolver = new DamageResolver(aggroResolver, gameMessageService, damagedEventBuffer, deathEventBuffer);
        var healResolver = new HealResolver(aggroResolver, gameMessageService, healedEventBuffer);
        var hitResolver = new HitResolver(gameMessageService);
        var hitDamageFactory = new HitDamageFactory();
        var weaponProcResolver = new WeaponProcResolver(logger, gameMessageService, intentBusContainer, effectRegistry);
        var reactionResolver = new ReactionResolver(gameMessageService);
        var effectExecutor = new EffectExecutor(damageResolver, healResolver);
        var effectFactory = new EffectFactory(logger, gameMessageService, intentBusContainer, effectExecutor);

        var actionOrchestrator = new ActionOrchestrator(logger, intentBusContainer, attackResolvedEventBuffer, effectResolvedEventBuffer, effectRegistry, effectFactory, hitResolver, hitDamageFactory, damageResolver, weaponProcResolver, reactionResolver);

        var commandExecutionSystem = new CommandExecutionSystem(logger);
        var fleeSystem = new FleeSystem(gameMessageService, intentBusContainer, fleeBlockedEventBuffer);
        var movementSystem = new MovementSystem(gameMessageService, intentBusContainer, movedEventBuffer);
        var itemInteractionSystem = new ItemInteractionSystem(gameMessageService, intentBusContainer, itemGotEventBuffer, itemDroppedEventBuffer, itemGivenEventBuffer, itemPutEventBuffer, itemWornEventBuffer, itemRemovedEventBuffer, itemDestroyedEventBuffer, itemSacrifierEventBuffer);
        var statsSystem = new EffectiveStatsSystem();
        var autoAttackSystem = new AutoAttackSystem(intentBusContainer);
        var deathSystem = new DeathSystem(gameMessageService, intentBusContainer, deathEventBuffer);
        var lootSystem = new LootSystem(gameMessageService, intentBusContainer, itemLootedEventBuffer);
        var lookSystem = new LookSystem(lookService, intentBusContainer, lookedEventBuffer);
        var cleanupSystem = new CleanupSystem(logger, effectFactory);

        //// subscribe to events for demo purposes
        //var fleeBlockedEventDispatcher = new EventDispatcher<FleeBlockedEvent>();
        //var damageEventDispatcher = new EventDispatcher<DamageEvent>();
        //var itemGotEventDispatcher = new EventDispatcher<ItemGotEvent>();
        //var itemDropEventDispatcher = new EventDispatcher<ItemDroppedEvent>();
        //var itemGivenEventDispatcher = new EventDispatcher<ItemGivenEvent>();
        //var itemPutEventDispatcher = new EventDispatcher<ItemPutEvent>();
        //var deathEventDispatcher = new EventDispatcher<DeathEvent>();
        //var itemLootedEventDispatcher = new EventDispatcher<ItemLootedEvent>();

        //fleeBlockedEventDispatcher.Subscribe(e => gameMessageService.To(e.Entity).Send($"You cannot flee: {e.Reason}."));
        //itemGotEventDispatcher.Subscribe(e => gameMessageService.To(e.Entity).Send($"You get {e.Item.DisplayName} from {e.RoomOrContainer.DisplayName}."));
        //itemDropEventDispatcher.Subscribe(e => gameMessageService.To(e.Entity).Send($"You get {e.Item.DisplayName} from {e.Room.DisplayName}."));
        //itemGivenEventDispatcher.Subscribe(e => gameMessageService.To(e.Entity).Send($"You give {e.Item.DisplayName} to {e.Target.DisplayName}."));
        //itemPutEventDispatcher.Subscribe(e => gameMessageService.To(e.Entity).Send($"You put {e.Item.DisplayName} in {e.Container.DisplayName}."));
        //damageEventDispatcher.Subscribe(e => gameMessageService.ToAll(e.Source).Act("%G{0} deal{0:v} %r{1}%g damage to {2}.%x").With(e.Source, e.Amount, e.Target));
        //deathEventDispatcher.Subscribe(e =>
        //{
        //    gameMessageService.To(e.Dead).Send("%RYou have been KILLED%x");
        //    gameMessageService.ToRoom(e.Dead).Act("{0} is dead").With(e.Dead);
        //});
        //itemLootedEventDispatcher.Subscribe(e => gameMessageService.To(e.Entity).Send($"You loot {e.Item.DisplayName} from {e.Corpse.DisplayName}."));

        //scenario 1: goblin is in temple square, chest with gem is in temple square, troll is in temple square. Goblin get gem from chest and troll attacks goblin 3 times.
        //// goblin takes gem from chest
        //ref var goblinGetIntent = ref intentBusContainer.GetItem.Add();
        //goblinGetIntent.Entity = goblin;
        //goblinGetIntent.Item = gem;
        //goblinGetIntent.RoomOrContainer = chest;
        //// troll attacks goblin 3 times
        //ref var trollAttack = ref intentBusContainer.Attack.Add();
        //trollAttack.Attacker = troll;
        //trollAttack.Target = goblin;
        //trollAttack.RemainingHits = 3;

        //// scenario 2: goblin is in temple square, chest with gem is in temple square, troll is in temple square. Goblin attacks troll 3 times but troll counterattacks and kills goblin.
        //ref var trollEffecticeStats = ref troll.Get<EffectiveStats>();
        //trollEffecticeStats.Dodge = 0; // for testing, make sure all hits land so we can see the counterattack in action
        //trollEffecticeStats.Parry = 0; // for testing, make sure all hits land so we can see the counterattack in action
        //trollEffecticeStats.CounterAttack = 100; // for testing, make sure all we counterattack every time so we can see the counterattack in action
        //// goblin attacks troll 3 times
        //ref var goblinAttack = ref intentBusContainer.Attack.Add();
        //goblinAttack.Attacker = goblin;
        //goblinAttack.Target = troll;
        //goblinAttack.RemainingHits = 3;

        //// scenario 3: goblin is in temple square, chest with gem is in temple square, troll is in temple square. Goblin (low life) uses command kill troll and troll has auto counterattacks
        //ref var trollEffectiveStats = ref troll.Get<EffectiveStats>();
        //trollEffectiveStats.Dodge = 0; // for testing, make sure all hits land so we can see the counterattack in action
        //trollEffectiveStats.Parry = 0; // for testing, make sure all hits land so we can see the counterattack in action
        //trollEffectiveStats.CounterAttack = 100; // for testing, make sure all we counterattack every time so we can see the counterattack in action
        //commandDispatcher.Dispatch(executionContext, gameState, goblin, "kill troll".AsSpan());
        //// goblin uses command kill troll
        ////goblin.Add(new CombatState { Target = troll, RoundDelay = 0 });
        ///
        // scenario 4: player applies 2 times armor on troll
        commandDispatcher.Dispatch(executionContext, gameState, player, "test troll armor".AsSpan());
        commandDispatcher.Dispatch(executionContext, gameState, player, "test troll armor".AsSpan());

        // start of gameloop

        // TODO: reset reaction budget for all entities at the start of tick
        //foreach (var entity in world.Query<ReactionBudget>())
        //{
        //    entity.ReactionBudget.Remaining = 1; // or based on stats
        //}

        // one tick
        commandExecutionSystem.Execute(executionContext, gameState);
        lookSystem.Tick(gameState, LookMode.Snapshot);
        fleeSystem.Tick(gameState);
        movementSystem.Tick(gameState);
        itemInteractionSystem.Tick(gameState);
        statsSystem.Tick(gameState);
        autoAttackSystem.Tick(gameState);
        actionOrchestrator.Tick(gameState);
        deathSystem.Tick(gameState);
        lootSystem.Tick(gameState);
        lookSystem.Tick(gameState, LookMode.PostUpdate);

        //// dispatch events for demo purposes
        //fleeBlockedEventDispatcher.Dispatch(fleeBlockedEventBuffer.GetAll());
        //itemGotEventDispatcher.Dispatch(itemGotEventBuffer.GetAll());
        //itemDropEventDispatcher.Dispatch(itemDroppedEventBuffer.GetAll());
        //itemGivenEventDispatcher.Dispatch(itemGivenEventBuffer.GetAll());
        //itemPutEventDispatcher.Dispatch(itemPutEventBuffer.GetAll());
        //damageEventDispatcher.Dispatch(damageEventBuffer.GetAll());
        //deathEventDispatcher.Dispatch(deathEventBuffer.GetAll());
        //itemLootedEventDispatcher.Dispatch(itemLootedEventBuffer.GetAll());

        cleanupSystem.Tick(gameState);

        intentBusContainer.ClearAll();
        fleeBlockedEventBuffer.Clear();
        itemGotEventBuffer.Clear();
        itemDroppedEventBuffer.Clear();
        itemGivenEventBuffer.Clear();
        itemPutEventBuffer.Clear();
        itemDestroyedEventBuffer.Clear();
        itemSacrifierEventBuffer.Clear();
        damagedEventBuffer.Clear();
        deathEventBuffer.Clear();
        itemLootedEventBuffer.Clear();
        lookedEventBuffer.Clear();

        // end of gameloop
    }

    private static void DumpWorld(World world)
    {
        Console.WriteLine("Dumping world state:");
        var query = new QueryDescription();
        world.Query(query, (Entity entity) =>
        {
            Console.WriteLine($"Entity Id: {entity.Id} Alive: {entity.IsAlive()} DebugName: {entity.DebugName}");
            Console.WriteLine($"  Components: {string.Join(", ", entity.GetAllComponents().Select(c => c?.GetType().Name))}");
        });
    }

    private class DemoMessageBus : IMessageBus
    {
        public void Publish(Entity entity, string message)
        {
            Console.WriteLine($"Message to {entity.DebugName}: {message}");
        }

        public void Process(GameState state)
        {
            // nop for demo
        }
    }
}
