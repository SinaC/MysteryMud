using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Logging;
using MysteryMud.Core.Scheduler;
using MysteryMud.Domain.Action;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Time;
using MysteryMud.Infrastructure.Services;

namespace MysteryMud.ConsoleApp.Hosting;

internal class GameLoop
{
    private readonly ILogger _logger;
    private readonly IOutputService _outputService;
    private readonly ICommandBus _commandBus;
    private readonly IMessageBus _messageBus;
    private readonly IScheduler _scheduler;
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intentContainer;
    private readonly EventBufferRegistry _buffers;
    private readonly World _world;

    // MaxResources cannot be injected
    private readonly ActionOrchestrator _actionOrchestrator;

    private readonly CommandExecutionSystem _commandExecutionSystem;
    private readonly CommandThrottleSystem _commandThrottleSystem;
    private readonly FleeSystem _fleeSystem;
    private readonly MovementSystem _movementSystem;
    private readonly ItemInteractionSystem _itemInteractionSystem;
    private readonly EffectiveCharacterStatsSystem _effectiveCharacterStatsSystem;
    private readonly EffectiveMaxResourceSystem<BaseHealth, Health, DirtyHealth, HealthModifier> _effectiveMaxHeathSystem;
    private readonly EffectiveMaxResourceSystem<BaseMana, Mana, DirtyMana, ManaModifier> _effectiveMaxManaSystem;
    private readonly EffectiveMaxResourceSystem<BaseEnergy, Energy, DirtyEnergy, EnergyModifier> _effectiveMaxEnergySystem;
    private readonly EffectiveMaxResourceSystem<BaseRage, Rage, DirtyRage, RageModifier> _effectiveMaxRageSystem;
    private readonly EffectiveResourceRegenSystem<HealthRegen, DirtyHealthRegen, HealthRegenModifier> _effectiveHeathRegenSystem;
    private readonly EffectiveResourceRegenSystem<ManaRegen, DirtyManaRegen, ManaRegenModifier> _effectiveManaRegenSystem;
    private readonly EffectiveResourceRegenSystem<EnergyRegen, DirtyEnergyRegen, EnergyRegenModifier> _effectiveEnergyRegenSystem;
    private readonly EffectiveResourceRegenSystem<RageDecay, DirtyRageDecay, RageDecayModifier> _effectiveRageRegenSystem;
    private readonly AbilityValidationSystem _abilityValidationSystem;
    private readonly AbilityCastingSystem _abilityCastingSystem;
    private readonly AbilityExecutionSystem _abilityExecutionSystem;
    private readonly AutoAttackSystem _autoAttackSystem;
    private readonly TimedEffectSystem _timedEffectSystem;
    private readonly ResourceRegenSystem<Health, HealthRegen> _healthRegenSystem;
    private readonly ResourceRegenSystem<Mana, ManaRegen> _manaRegenSystem;
    private readonly ResourceRegenSystem<Energy, EnergyRegen> _energyRegenSystem;
    private readonly ResourceRegenSystem<Rage, RageDecay> _rageDecaySystem;
    private readonly ThreatDecaySystem _threatDecaySystem;
    private readonly ScheduleSystem _scheduleSystem;
    private readonly DeathSystem _deathSystem;
    private readonly RespawnSystem _respawnSystem;
    private readonly LootSystem _lootSystem;
    private readonly LookSystem _lookSystem;
    private readonly CleanupSystem _cleanupSystem;

    public GameLoop(
        ILogger logger,
        IOutputService putputService,
        ICommandBus commandBus,
        IMessageBus messageBus,
        IScheduler scheduler,
        IGameMessageService gameMessageService,
        IIntentContainer intentContainer,
        EventBufferRegistry eventBufferRegistry,
        World world,
        ActionOrchestrator actionOrchestrator,
        CommandExecutionSystem commandExecutionSystem,
        CommandThrottleSystem commandThrottleSystem,
        FleeSystem fleeSystem,
        MovementSystem movementSystem,
        ItemInteractionSystem itemInteractionSystem,
        EffectiveCharacterStatsSystem effectiveCharacterStatsSystem,
        EffectiveMaxResourceSystem<BaseHealth, Health, DirtyHealth, HealthModifier> effectiveMaxHeathSystem,
        EffectiveMaxResourceSystem<BaseMana, Mana, DirtyMana, ManaModifier> effectiveMaxManaSystem,
        EffectiveMaxResourceSystem<BaseEnergy, Energy, DirtyEnergy, EnergyModifier> effectiveMaxEnergySystem,
        EffectiveMaxResourceSystem<BaseRage, Rage, DirtyRage, RageModifier> effectiveMaxRageSystem,
        EffectiveResourceRegenSystem<HealthRegen, DirtyHealthRegen, HealthRegenModifier> effectiveHeathRegenSystem,
        EffectiveResourceRegenSystem<ManaRegen, DirtyManaRegen, ManaRegenModifier> effectiveManaRegenSystem,
        EffectiveResourceRegenSystem<EnergyRegen, DirtyEnergyRegen, EnergyRegenModifier> effectiveEnergyRegenSystem,
        EffectiveResourceRegenSystem<RageDecay, DirtyRageDecay, RageDecayModifier> effectiveRageRegenSystem,
        AbilityValidationSystem abilityValidationSystem,
        AbilityCastingSystem abilityCastingSystem,
        AbilityExecutionSystem abilityExecutionSystem,
        AutoAttackSystem autoAttackSystem,
        TimedEffectSystem timedEffectSystem,
        ResourceRegenSystem<Health, HealthRegen> healthRegenSystem,
        ResourceRegenSystem<Mana, ManaRegen> manaRegenSystem,
        ResourceRegenSystem<Energy, EnergyRegen> energyRegenSystem,
        ResourceRegenSystem<Rage, RageDecay> rageDecaySystem,
        ThreatDecaySystem threatDecaySystem,
        ScheduleSystem scheduleSystem,
        DeathSystem deathSystem,
        RespawnSystem respawnSystem,
        LootSystem lootSystem,
        LookSystem lookSystem,
        CleanupSystem cleanupSystem)
    {
        _logger = logger;
        _outputService = putputService;
        _commandBus = commandBus;
        _messageBus = messageBus;
        _scheduler = scheduler;
        _msg = gameMessageService;
        _intentContainer = intentContainer;
        _buffers = eventBufferRegistry;
        _world = world;

        _actionOrchestrator = actionOrchestrator;
        _commandExecutionSystem = commandExecutionSystem;
        _commandThrottleSystem = commandThrottleSystem;
        _fleeSystem = fleeSystem;
        _movementSystem = movementSystem;
        _itemInteractionSystem = itemInteractionSystem;
        _effectiveCharacterStatsSystem = effectiveCharacterStatsSystem;
        _effectiveMaxHeathSystem = effectiveMaxHeathSystem;
        _effectiveMaxManaSystem = effectiveMaxManaSystem;
        _effectiveMaxEnergySystem = effectiveMaxEnergySystem;
        _effectiveMaxRageSystem = effectiveMaxRageSystem;
        _effectiveHeathRegenSystem = effectiveHeathRegenSystem;
        _effectiveManaRegenSystem = effectiveManaRegenSystem;
        _effectiveEnergyRegenSystem = effectiveEnergyRegenSystem;
        _effectiveRageRegenSystem = effectiveRageRegenSystem;
        _abilityValidationSystem = abilityValidationSystem;
        _abilityCastingSystem = abilityCastingSystem;
        _abilityExecutionSystem = abilityExecutionSystem;
        _autoAttackSystem = autoAttackSystem;
        _timedEffectSystem = timedEffectSystem;
        _manaRegenSystem = manaRegenSystem;
        _energyRegenSystem = energyRegenSystem;
        _rageDecaySystem = rageDecaySystem;
        _healthRegenSystem = healthRegenSystem;
        _threatDecaySystem = threatDecaySystem;
        _scheduleSystem = scheduleSystem;
        _deathSystem = deathSystem;
        _respawnSystem = respawnSystem;
        _lootSystem = lootSystem;
        _lookSystem = lookSystem;
        _cleanupSystem = cleanupSystem;
    }

    public void Run()
    {
        _logger.LogInformation(LogEvents.System, "Starting game loop");

        var currentTick = 0;

        while (true)
        {
            CheckConsoleInput();

            Tick(currentTick);

            Thread.Sleep(TimeRate.TicksInMilliseconds); // tick rate

            currentTick++;
        }
    }

    private void Tick(int currentTick)
    {
        //_logger.LogDebug("TICK: {currentTick}", currentTick);
        using (_logger.BeginScope(new Dictionary<string, object> { ["TICK"] = currentTick }))
        {
            var state = new GameState
            {
                World = _world,
                CurrentTick = currentTick,
                CurrentTimeMs = currentTick * TimeRate.TicksInMilliseconds
            };

            // Player commands 
            _commandBus.Process(state);
            // check spam, wait state, can cancel command
            _commandThrottleSystem.Execute(state);
            // execute command (if not cancelled) → may generate manual LookIntent(Mode= Snapshot)
            _commandExecutionSystem.Execute(state);
            // TODO: stop invalid combat: dead entities, entities in different rooms, ...
            // Process all LookIntents with Mode=Snapshot → reads current world state before any effects → produces messages
            _lookSystem.Tick(state, LookMode.Snapshot);
            // TODO: AISystem                         // NPC behavior → generates intents
            // Convert flee → MoveIntents
            _fleeSystem.Tick(state);
            // TODO: ChaseSystem                      // NPC chase movement
            // Process MoveIntents → emits auto-look PostUpdate (Mode=PostUpdate)
            _movementSystem.Tick(state);
            // Process get/drop/put/give/...
            _itemInteractionSystem.Tick(state);
            // Recalculate stats with DirtyStats
            _effectiveCharacterStatsSystem.Tick(state);
            // Recalculate resource with DirtyResource where resource can be Health, Mana, Energy, Rage
            _effectiveMaxHeathSystem.Tick(state);
            _effectiveMaxManaSystem.Tick(state);
            _effectiveMaxEnergySystem.Tick(state);
            _effectiveMaxRageSystem.Tick(state);
            // Recalculate resource regen with DirtyResourceRegen where resource can be Health, Mana, Energy, Rage
            _effectiveHeathRegenSystem.Tick(state);
            _effectiveManaRegenSystem.Tick(state);
            _effectiveEnergyRegenSystem.Tick(state);
            _effectiveRageRegenSystem.Tick(state);
            // Generate triggered scheduled event (tick or expired)
            _scheduler.Process(state);
            // Resolve triggered scheduled event and generates scheduleIntent (for next tick), effectExpiredEvent (to inform), effectTickedEvent (to inform)
            _timedEffectSystem.Tick(state);
            // Regen resource
            if (state.CurrentTick % TimeRate.TicksPerSecond == 0) // once by second
            {
                _manaRegenSystem.Tick(state);
                _energyRegenSystem.Tick(state);
                _rageDecaySystem.Tick(state);
                _healthRegenSystem.Tick(state);
            }
            // Decay threat by 2%
            _threatDecaySystem.Tick(state);
            // TODO: NPCTargetSystem.AssignTargets   // Select highest threat targets
            // TODO: GroupCombatSystem.Resolve       // Handle assist/protect/own target attack intents
            // Process ResolvedAbilityIntents -> set casting (if delayed casting) or generate ExecuteAbilityIntent (instant cast)
            _abilityValidationSystem.Tick(state);
            // Process delayed casting, once cast is effective generate ExecuteAbilityIntent + abilityUsedEvent
            _abilityCastingSystem.Tick(state);
            // Process ExecuteAbilityIntents -> generate ActionIntent(kind:effect) for each effects in ability + abilityExecutedEvent
            _abilityExecutionSystem.Tick(state);
            // Generate AttackIntents for entities in combat
            _autoAttackSystem.Tick(state);
            // Process ActionIntents. kind:attack -> resolve hit, perform damage, check weapon proc (effect), check reaction (counter attack)  kind: effect -> resolve effect
            _actionOrchestrator.Tick(state);
            // Process dead entities: remove from combat, remove casting, create corpse -> generate loot intent
            _deathSystem.Tick(state);
            // Auto-resurrect players
            _respawnSystem.Tick(state);
            // Handle loot & auto-loot
            _lootSystem.Tick(state);
            // Process LookIntents with Mode=PostUpdate → reflects final world state after all updates
            _lookSystem.Tick(state, LookMode.PostUpdate);
            // Handle scheduleIntents (which can be generated from IA, abilities, TimedEffectSytem, AttackOrchestrator)
            _scheduleSystem.Tick(state);

            // Remove destroyed items / dead NPCs / disconnected players
            _cleanupSystem.Tick(state);

            // Process messages to be sent to players
            _messageBus.Process(state);

            // Send messages to players
            _outputService.FlushAll();

            // Clear intents
            _intentContainer.ClearAll();
            // Clear event buffers
            _buffers.ClearAll();
        }
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