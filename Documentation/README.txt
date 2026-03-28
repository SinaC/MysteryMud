Project structure

Layer             Role
GameData          Pure data: positions, enums, spell/effect definitions, constants. No behavior. Immutable.
Core              Fundamental abstractions for your system: interfaces, command definitions, priorities. Agnostic to runtime ECS.
Domain            ECS components, systems, factories, domain logic (mutable runtime state).
Application       Parser and concrete command implementations.
Infrastructure    Registries, persistence, networking, scheduler, eventing, dispatcher.
ConsoleApp        Entry point, console I/O, game loop, game server.

Tick pipeline

1. Input → Commands → Intents (player & AI)
2. AISystem                      // NPC behavior, emits intents
3. AggroSystem                    // Auto-attack based on aggro
4. FleeSystem                     // Convert flee intent → move intent
5. ChaseSystem                     // NPC chase movement
6. MovementSystem                  // Resolve all MoveIntents
7. InteractionSystem               // get/drop/put/give
8. StatSystem                       // recalc stats from DirtyStats components (was originally 9)
9. DOT/HOT/EffectDurationSystem    // apply scheduled effects (damage, heal, buffs/debuffs) (was originally 8)
10. ThreatSystem.UpdateThreat       // from damage, heal, buffs
11. NPCTargetSystem.AssignTargets   // select high threat targets
12. GroupCombatSystem.Resolve       // handle assist/protect/own target attack intents
13. AbilitySystem                   // spells, skill usage
14. CombatOrchestrator              // AttackIntents → AttackEvents + reactions, procs, spell effects, damage, heal, etc. Loop until no more AttackIntents or max depth reached
15. DeathSystem                     // detect deaths
16. RespawnSystem                   // auto-resurrect players
17. LootSystem                       // handle loot & auto-loot
18. CleanupSystem                    // remove destroyed items / dead NPCs
19. Output → MessageBus             // send updates to players

CombatOrchestrator (step 14) details
    HitPhase / IntentResolution
        Determine hit, dodge, parry
        Generate AttackResolved events
        Produce messages like “You dodged!”
    ProduceDamage
        Converts AttackResolved(Hit) into DamageEvent
        Sets SourceType = Hit (or Spell, DoT, etc.)
        No reactions here
    DamageSystem
        Applies HP changes
        Generates death events if HP ≤ 0
        Sends messages like “You take 5 damage”
        Does not trigger counterattack
    ReactionPhase
        Loops over AttackResolved events (or potentially damage events if needed)
        Checks conditions: parry -> guaranteed counter, hit -> chance to counter
        Generates new AttackIntents for counterattacks
        These intents go into Next buffer, which is processed in the same tick

Entity/Components

Character
  ├Location: room
  ├BaseStats: level, experience, dictionary stat/value
  ├EffectiveStats: dictionary stat/value
  ├CharacterEffects: effect list, effects by tag, active tags
  ├Inventory: list of items
  ├Equipment: list of equipped items
  └Health: current and max health
  optional
    DirtyStats: needs effective stats recalculated
    CombatState: in combat
    DeadTag: is dead
    Gender: male|female|neutral
    Mana: current and max mana

Npc(character+)
  ├NpcTag
  └ThreatTable: list of characters and threat values for aggro

Player(character+)
  ├PlayerTag
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
 ├ EffectInstance: Source, Target, Template, StackCount
 ├ Duration: StartTick, ExpiredTick
 ├ EffectTag: EffectTagId
 ├ StatModifiers: StatModifier list
 ├ DamageOverTime: Damage, DamageType, TickRate, NextTick;
 └ HealOverTime: Heal, TickRate, NextTick;

Datas

EffectTemplate:
    Name: name of the effect template (e.g. "Strength Buff")
    EffectTag: EffectTagId
    StackingRule: None|Replace|ExtendDuration|ReplaceIfStronger
    AffectFlags: bitflags for quick checks (e.g. is buff, is debuff, is dispellable) (TODO)
    MaxStacks: maximum number of stacks (if stacking is allowed)
    StatModifiers: StatModifierDefinition list
    DotFunction: function to calculate damage for damage over time effects (returns DotDefinition)
    HotFunction: function to calculate healing for heal over time effects (returns HotDefinition)
    ApplyMessage: message to show when the effect is applied
    WearOffMessage: message to show when the effect wears off