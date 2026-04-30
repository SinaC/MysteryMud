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
1. CommandBus.Process               // Read player inputs from command bus, extract command, check existence/permission/position and add CommandRequests in player CommandBuffer + set tag HasCommand
2. CommandThrottleSystem            // Check spam, wait state, can cancel command for each entity with HasCommandTag
3. CommandExecutionSystem           // Execute command (if not cancelled) -> execute directly or set combat state or generate look intent or generate effect intent or item interaction intents or other intents
4. AutoAssistSystem (pass 1)        // Catches player-initiated combat (checking NewCombatant tag)
5. LookSystem(Snapshot)             // Process all LookIntents with Mode=Snapshot -> reads current world state before any effects -> produces messages
6. AISystem                         // NPC behavior -> generates intents
7. FleeSystem                       // Convert flee -> MoveIntents
8. ChaseSystem                      // NPC chase movement
9. FollowSystem                     // Handle characters following another character, scan MoveIntents from leaders and generate MoveIntents for followers
10. MovementSystem                  // Process MoveIntents -> emits auto-look PostUpdate (Mode=PostUpdate)
11. AutoAssistSystem (pass 2)       // Catches room-entry assists, consumes RoomEnteredEvent
12. ItemInteractionSystem           // Process get/drop/put/give/... intents
13. EffectiveIRVSystem              // Recalculate immunities/resistances/vulnerabilities from base and modifiers (only if DirtyIRV tag is set)
14. EffectiveStatSystem             // Recalculate stats from base stats and stat modifiers (only if DirtyStats tag is set)
15. EffectiveMaxResourcesSystem     // Recalculate max health/mana/energy/rage from base max health/mana/energy/rage and resource modifiers (only if DirtyHealth/mana/energy/rage tag is set) (4 separate systems)
16. EffectiveResourceRegenSystem    // Recalculate current health/mana/energy/rage regen from base health/mana/energy/rage regen and resource regen modifiers (only if DirtyHealthRegen/mana/energy/rage tag is set) (4 separate systems)
17. Scheduler                       // Generate triggered scheduled event (tick or expired)
18. TimedEffectSystem               // Resolve triggered scheduled event and generates scheduleIntent, effectExpiredEvent (to inform), effectTickedEvent (to inform)
19. ResourceRegenSystem             // Regen health/mana/energy decay rage (4 separate systems)
20. ThreatDecaySystem               // Decay threat
21. AbilityValidationSystem         // Process UseAbilityIntents -> set casting (if delayed casting) or generate ExecuteAbilityIntent (instant cast)
22. AbilityCastingSystem            // Process delayed casting, once cast is effective generate ExecuteAbilityIntent + abilityUsedEvent
23. AbilityExecutionSystem          // Process ExecuteAbilityIntents -> generate ActionIntent(kind:effect) for each effects in ability + abilityExecutedEvent
24. NPCTargetSystem                 // Select highest threat targets
25. GroupTacticsSystem              // Protect, Assist targeting, Group target coordination
26. AutoAttackSystem                // Generate ActionIntent(kind:attack) for every entity in combat (CombatState component set)
27. ActionOrchestrator              // Process ActionIntents. kind:attack -> resolve hit, perform damage, check weapon proc (effect), check reaction (counter attack)  kind: effect -> resolve effect
28. AggressionSystem                // Process AggressedEvent -> set CombatState+NewCombatantTag
29. AutoAssistSystem (pass 3)       // Catches mid-round combat triggers (checking NewCombatant tag)
30. DeathSystem                     // Flag dead entities
31. RespawnSystem                   // Auto-resurrect players
32. LootSystem                      // Process CorpseLootIntents
33. AutoSacrificeSystem             // Process AutoSacrificeIntents
34. LookSystem(PostUpdate)          // Process LookIntents with Mode=PostUpdate -> reflects final world state after all updates
35. ScheduleSystem                  // Process scheduleIntents (which can be generated from IA, TimedEffectSytem and ActionOrchestrator)
36. PersistenceSystem               // Persist entities flags as to persist using DirtyTracker
37. DisconnectSystem                // calls Disconnect(entity) → socket closes + GameServer.HandleDisconnected fires async + sets DisconnectedTag
38. CleanupSystem                   // Remove destroyed items / dead NPCs / disconnected players
39. Output -> MessageBus            // Send all messages to players

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
        Loop on killRewards and reward xp for killer and group members

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
  ├BaseStats: fixed-array of stat/value
  ├EffectiveStats: fixed-array of stat/value
  ├CharacterEffects: effect list, effects by tag, active tags
  ├Inventory: list of items
  ├Equipment: list of equipped items
  ├Form: current form: humanoid, cat, bear
  ├BaseHealth: base max health
  ├Health: current and max health
  ├DirtyHealth: max health needs to be recalculated
  └HealthRegen: current and base health regen rate
  optional
    HasCommandTag: a command (or more) is waiting in CommandBuffer
    DirtyStats: needs effective stats to be recalculated
    DirtyResources: needs effective resources to be recalculated
    CombatState: in combat
    CombatInitiator: what are the combat claim for this entity
    NewCombatantTag: just entered a fight
    DeadTag: is dead? will be removed by cleanup system if no RespawnState found
    Gender: male|female|neutral
    BaseMana, Mana, ManaRegen, UsesMana, DirtyMana, DirtyManaRegen: base max mana, current and max mana, current and base mana regen rate, can use mana, max mana needs to be recalculated, mana regen needs to be recalculated
    same for energy, rage
    Followers: list of characters following
    Following: who character is following

Npc(character+)
  ├NpcTag
  └ThreatTable: list of characters and threat values for aggro
  optional
    AutoCommand: last command from Npc
    ActiveThreatTag: set while there is an active entry in ThreatTable
    Charmed: charmed by who
    NpcAssistBehavior: how this NPC will assist players and/or other NPCs

Player(character+)
  ├PlayerTag
  ├Progression: total experience, experience by level
  ├AutoBehavior: auto loot, auto sac, auto assist
  ├CommandThrottle: command throttling information to detect spam
  └Connection
  optional
    GroupMember: which group player belongs
    RespawnState: respawn timer and location when a player dies
    DisconnectedTag: is disconnected
    Charmies: list of charmed characters

Group
  └GroupInstance: leader, group members, loot rule, loot master

Item
  ├ItemTag
  └ItemEffects: effec list, effects by tag, active tags
  optional
    Container: set when item is a container, store max capacity
    ContainerContents: list of items contained if item is a container
    ContainedIn: when in a container, store container entity
    Equipable: is item equipable and which slot
    Equipped: is item equipped and which wearer/slot
    ItemOwner: item owner if any
    DestroyedTag: is destroyed ? will be remove from world by CleanupSystem
    Weapon: is a weapon ? kind, dice count, dice value, list of proc id

Room
  ├RoomTag
  ├RoomContents: list of characters and items in this room
  └RoomGraph: list of exits

Zone
    not yet implemented

Effect (not stacking if difference source)
 ├EffectInstance: Source, Target, Definition, StackCount
 ├TimedEffect: TickRate (= 0 means pure duration effect), NextTick, StartTick, ExpirationTick, LastRefreshTick
 ├CharacterEffectTag: character EffectTagId (bit fields)
 ├ItemEffectTag: character EffectTagId (bit fields)
 ├StatModifiers: StatModifier list
 ├ResourceModifiers: ResourceModifier list
 ├ResourceRegenModifiers: ResourceRegenModifier list
 └EffectValuesSnapshot: snapshotted values
 optional
    ExpiredTag: expired will be removed by cleanup system