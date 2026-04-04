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

1. Input → Commands → Intents       // Player commands → may generate manual LookIntent (Mode=Snapshot) and other intents
2. LookSystem(Snapshot)             // Process all LookIntents with Mode=Snapshot → reads current world state before any effects → produces messages
3. AISystem                         // NPC behavior → generates intents
4. FleeSystem                       // Convert flee → MoveIntents
5. ChaseSystem                      // NPC chase movement
6. MovementSystem                   // Process MoveIntents → emits auto-look PostUpdate (Mode=PostUpdate)
7. InteractionSystem                // Process get/drop/put/give/... intents
8. StatSystem                       // Recalculate stats from DirtyFlags
9. Scheduler.Process                // Generate triggered scheduled event (tick or expired)
10. TimedEffectSystem               // Resolve triggered scheduled event and generates scheduleIntent, effectExpiredEvent (to inform), effectTickedEvent (to inform)
11. ThreatDecaySystem               // Decay threat
12. NPCTargetSystem                 // Select highest threat targets
13. GroupCombatSystem.Resolve       // Process assist/protect/own target attack intents
14. AutoAttackSystem                // Generate attack intents for every entity in combat (CombatState component set)
15. CombatOrchestrator              // Process AttackIntents (resolve damage/heal/aggro) → AttackEvents + reactions
16. DeathSystem                     // Flag dead entities
17. RespawnSystem                   // Auto-resurrect players
18. LootSystem                      // Process loot & auto-loot
19. LookSystem(PostUpdate)          // Process LookIntents with Mode=PostUpdate → reflects final world state after all updates
20. ScheduleSystem                  // Process scheduleIntents (which can be generated from IA, abilities, TimedEffectSytem, AttackOrchestrator)
21. CleanupSystem                   // Remove destroyed items / dead NPCs / disconnected players
22. Output → MessageBus             // Send all messages to players

CombatOrchestrator (step 15) details
   loop on attack intents
        ResolveHit 
             Determine hit, dodge, parry -> ResolvedHit 
             Generate AttackResolvedEvent
             Produce messages like “You dodged!”
         ResolveDamage (if hit)
             Applies HP/Threat changes
             Generates death events if HP ≤ 0
             Sends messages like “You take 5 damage”
             Does not trigger counterattack
         ReactionPhase (if victim is still alive)
             Checks conditions: parry -> guaranteed counterattack, hit -> chance to counterattack
             Generates new AttackIntents for counterattacks
         MultiHitPhase
             Generate one AttackIntent (with one less remaining hit) if there are remaining hits for this attack round

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
│     └─ Player sends text commands → CommandBus.Publish() → CommandDispatcher Generate CommandRequest -> Player's CommandBuffer
│
├─> 2. AI System (Domain)
│     └─ Loop over NPCs:
│           ├─ Skip NPC if (CurrentTime - LastAITick < TickRate)
│           └─ Otherwise, Generate CommandRequest(s) → NPC's CommandBuffer
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

Entity/Components

Character
  ├Name
  ├CommandBuffer: list of pending commands
  ├Location: room
  ├BaseStats: level, experience, dictionary stat/value
  ├EffectiveStats: dictionary stat/value
  ├CharacterEffects: effect list, effects by tag, active tags
  ├Inventory: list of items
  ├Equipment: list of equipped items
  └Health: current and max health
  optional
    HasCommandTag: a command (or more) is waiting in CommandBuffer
    DirtyStats: needs effective stats recalculated
    CombatState: in combat
    DeadTag: is dead will be removed by cleanup system
    Gender: male|female|neutral
    Mana: current and max mana

Npc(character+)
  ├NpcTag
  └ThreatTable: list of characters and threat values for aggro
  optional
    AutoCommand: last command from Npc
    ActiveThreatTag: set while there is an active entry in ThreatTable

Player(character+)
  ├PlayerTag
  ├CommandThrottle
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
 ├ DamageEffect: Damage, DamageKind
 └ HealEffect: Heal
 optional
    ExpiredTag: expired will be removed by cleanup system

Datas

EffectDefinition:
    Id: name of the effect template (e.g. "Strength Buff")
    EffectTag: EffectTagId
    StackingRule: None|Replace|ExtendDuration|ReplaceIfStronger
    MaxStacks: maximum number of stacks (if stacking is allowed)
    AffectFlags: bitflags for quick checks (e.g. is buff, is debuff, is dispellable) (TODO)
    DurationFunc: function to calculate duration of the effect (in tick)
    TickRate: if 0 -> pure duration effect
    TickOnApply: if true -> apply immediately first tick
    StatModifiers: StatModifierDefinition list
    DotDefinition: dot definition if any
    HotDefinition: hot definition if any
    ApplyMessage: message to show when the effect is applied
    WearOffMessage: message to show when the effect wears off
DotDefinition
    DamageFunc: function to calculate damage
    DamageKind: kind of damage
HotDefinition
    HealFunc: function to calculate heal