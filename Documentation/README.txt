Project structure
Layer             Role
GameData          Pure data: positions, enums, spell/effect definitions, constants. No behavior. Immutable.
Core              Fundamental abstractions for your system: interfaces, command definitions, priorities. Agnostic to runtime ECS.
Domain            ECS components, systems, factories, domain logic (mutable runtime state).
Application       Parser and concrete command implementations.
Infrastructure    Registries, persistence, networking, scheduler, eventing, dispatcher.
ConsoleApp        Entry point, console I/O, game loop, game server.
each layer depends on the previous one

Tick pipeline
1. Input -> Commands -> Intents     // Player commands -> may generate manual LookIntent (Mode=Snapshot) and other intents
2. LookSystem(Snapshot)             // Process all LookIntents with Mode=Snapshot -> reads current world state before any effects -> produces messages
3. AISystem                         // NPC behavior -> generates intents
4. FleeSystem                       // Convert flee -> MoveIntents
5. ChaseSystem                      // NPC chase movement
6. MovementSystem                   // Process MoveIntents -> emits auto-look PostUpdate (Mode=PostUpdate)
7. ItemInteractionSystem            // Process get/drop/put/give/... intents
8. EffectiveStatSystem              // Recalculate stats from base stats and stat modifiers (only if DirtyStats tag is set)
9. EffectiveMaxResourcesSystem      // Recalculate max health/mana/energy/rage from base max health/mana/energy/rage and resource modifiers (only if DirtyHealth/mana/energy/rage tag is set) (4 separate systems)
10. EffectiveRespirceRegenSystem    // Recalculate current health/mana/energy/rage regen from base health/mana/energy/rage regen and resource regen modifiers (only if DirtyHealthRegen/mana/energy/rage tag is set) (4 separate systems)
11. Scheduler.Process               // Generate triggered scheduled event (tick or expired)
12. TimedEffectSystem               // Resolve triggered scheduled event and generates scheduleIntent, effectExpiredEvent (to inform), effectTickedEvent (to inform)
13. ResourceRegenSystem             // Regen health/mana/energy decay rage (4 separate systems)
14. ThreatDecaySystem               // Decay threat
15. AbilityValidationSystem         // Process UseAbilityIntents -> set casting (if delayed casting) or generate ExecuteAbilityIntent (instant cast)
16. AbilityCastingSystem            // Process delayed casting, once cast is effective generate ExecuteAbilityIntent + abilityUsedEvent
17. AbilityExecutionSystem          // Process ExecuteAbilityIntents -> generate ActionIntent(kind:effect) for each effects in ability + abilityExecutedEvent
18. NPCTargetSystem                 // Select highest threat targets
19. GroupCombatSystem.Resolve       // Process assist/protect/own target attack intents
20. AutoAttackSystem                // Generate ActionIntent(kind:attack) for every entity in combat (CombatState component set)
21. ActionOrchestrator              // Process ActionIntents. kind:attack -> resolve hit, perform damage, check weapon proc (effect), check reaction (counter attack)  kind: effect -> resolve effect
22. DeathSystem                     // Flag dead entities
23. RespawnSystem                   // Auto-resurrect players
24. LootSystem                      // Process loot & auto-loot
25. LookSystem(PostUpdate)          // Process LookIntents with Mode=PostUpdate -> reflects final world state after all updates
26. ScheduleSystem                  // Process scheduleIntents (which can be generated from IA, abilities, TimedEffectSytem, AttackOrchestrator)
27. CleanupSystem                   // Remove destroyed items / dead NPCs / disconnected players
28. Output -> MessageBus            // Send all messages to players

ActionOrchestrator (step 21) details
   loop on attack/effect intents
        Attack intent
            ResolveHit 
                 Determine hit, dodge, parry -> ResolvedHit 
                 Generate AttackResolvedEvent
                 Produce messages like “You dodged!”
             ResolveDamage (if hit)
                 Applies HP/Threat changes
                 Generates death events if HP ≤ 0
                 Sends messages like “You take 5 damage”
                 Check weapon procs (weapon procs are not a reaction to an hit but a continuation of a hit)
             ReactionPhase (if victim is still alive)
                 Checks conditions: parry -> guaranteed counterattack, hit -> chance to counterattack
                 Generates new AttackIntents for counterattacks
             MultiHitPhase
                 Generate one AttackIntent (with one less remaining hit) if there are remaining hits for this attack round
        Effect intent
            ResolveEffect
                Stack effect if already exists
                Create effect otherwiseµ
                schedule expired effect event if duration is not null
                schedule tick effect event if tick rate > 0
                execute onApply actions

 ┌─────────────────────────┐
 │   Player inputs command │
 └─────────────┬───────-───┘
               │
               ▼
 ┌─────────────────────────┐
 │  CommandBus             │
 │  - Enqueue player input │
 └─────────────┬────────-──┘
               │
               ▼
 ┌─────────────────────────┐
 │  CommandDispatcher      │
 │  - Parses input         │
 │  - Looks up command     │
 │  - Enqueues into        │
 │    CommandBuffer        │
 │  - Adds HasCommandTag   │
 └─────────────┬────────-──┘
               │
               ▼
 ┌─────────────────────--------────┐
 │  CommandThrottleSystem          │
 │  - Prune old history            │
 │  - Refill per-category          │
 │    token buckets                │
 │  - Spam detection               │
 │    (MAX_IDENTICAL)              │
 │  - WAIT_STATE (NextAllowedTime) │
 │  - Cancel commands if blocked   │
 │  - Otherwise set ExecuteAt      │
 └─────────────┬─────---------─────┘
               │
               ▼
 ┌───────────────────────---------──┐
 │  CommandExecutionSystem          │
 │  - Iterates buffer               │
 │  - Skip cancelled                │
 │  - Skip ExecuteAt > now          │
 │  - Execute allowed cmds          │
 │  - Compact buffer for            │
 │    remaining commands            │
 │  - Remove HasCommandTag if empty │
 └─────────────┬──────────----------┘
               │
               ▼
 ┌─────────────────────────┐
 │ Command executed effects│
 │ (Combat, Movement, Chat │
 │  etc.)                  │
 └─────────────────────────┘

Tick Start
│
├─> 1. Player Input (Console / Network)
│     └─ Player sends text commands -> CommandBus.Publish() -> CommandDispatcher Generate CommandRequest -> Player's CommandBuffer
│
├─> 2. AI System (Domain)
│     └─ Loop over NPCs:
│           ├─ Skip NPC if (CurrentTime - LastAITick < TickRate)
│           └─ Otherwise, Generate CommandRequest(s) -> NPC's CommandBuffer
│
├─> 3. CommandThrottleSystem (Domain)
│     └─ Loop over entities with CommandBuffer & HasCommand tag
│           ├─ Anti-spam check
│           ├─ Per-command cooldown
│           └─ WAIT_STATE (global delay)
│           └─ Mark CommandRequest.Cancelled = true if blocked
│
├─> 4. CommandExecutionSystem (Domain)
│     └─ Loop over entities with CommandBuffer & HasCommand
│           ├─ Batch execution: MaxEntitiesPerTick
│           ├─ For each CommandRequest:
│           │     ├─ Execute command
│           │     └─ Generate IntentComponent (AttackIntent, MoveIntent, etc.)
│           └─ Clear CommandBuffer, remove HasCommand tag
│
├─> 5. Intent Processing Systems (Domain)
│     └─ Execute all intents generated by players & NPCs
│           ├─ CombatSystem
│           ├─ MovementSystem
│           └─ Spell/AbilitySystem
│
├─> 6.rest of tick pipeline
│
└─> Tick End

Ability targeting (*) for default value
    Requirement: Mandatory(*), Optional, None
    Selection: Single(*), AoE
    if single context
        Scope: Room(*), World, Inventory, Self
        Filter: None, Player, NPC, Item, (*)Character=Player+NPC, Any=Player+NPC+Item
    if multiple context
        Contexts: ordered list of
            Scope: Room, World, Inventory, Self
            Filter: None, Player, NPC, Item, (*)Character=Player+NPC, Any=Player+NPC+Item
    ResolveAt: CastStart(*), CastCompletion

Entity/Components

Character
  ├Name
  ├Level
  ├CommandBuffer: list of pending commands
  ├Location: room
  ├Position: position
  ├BaseStats: level, experience, dictionary stat/value
  ├EffectiveStats: dictionary stat/value
  ├CharacterEffects: effect list, effects by tag, active tags
  ├Inventory: list of items
  ├Equipment: list of equipped items
  ├BaseHealth: base max health
  ├Health: current and max health
  ├DirtyHealth: max health needs to be recalculated
  └HealthRegen: current and base health regen rate
  optional
    HasCommandTag: a command (or more) is waiting in CommandBuffer
    DirtyStats: needs effective stats to be recalculated
    DirtyResources: needs effective resources to be recalculated
    CombatState: in combat
    DeadTag: is dead will be removed by cleanup system
    Gender: male|female|neutral
    Form: current form (humanoid, bear, cat)
    BaseMana, Mana, ManaRegen, UsesMana, DirtyMana, DirtyManaRegen: base max mana, current and max mana, current and base mana regen rate, can use mana, max mana needs to be recalculated, mana regen needs to be recalculated
    same for energy, rage

Npc(character+)
  ├NpcTag
  └ThreatTable: list of characters and threat values for aggro
  optional
    AutoCommand: last command from Npc
    ActiveThreatTag: set while there is an active entry in ThreatTable

Player(character+)
  ├PlayerTag
  ├Progression: total experience, experience by level
  ├CommandThrottle: command throttling information to detect spam
  └Connection
  optional
    RespawnState: respawn timer and location when a player dies
    DisconnectedTag: is disconnected

Item
    todo
Room
    todo
Zone
    todo

Effect (not stacking if difference source)
 ├ EffectInstance: Source, Target, Definition, StackCount
 ├ TimedEffect: TickRate (= 0 means pure duration effect), NextTick, StartTick, ExpirationTick, LastRefreshTick
 ├ EffectTag: EffectTagId (bit fields)
 ├ StatModifiers: StatModifier list
 ├ ResourceModifiers: ResourceModifier list
 ├ ResourceRegenModifiers: ResourceRegenModifier list
 ├ DamageEffect: Damage, DamageKind
 └ HealEffect: Heal
 optional
    ExpiredTag: expired will be removed by cleanup system