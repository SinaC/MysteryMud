Layer             Role
GameData          Pure data: positions, enums, spell/effect definitions, constants. No behavior. Immutable.
Core              Fundamental abstractions for your system: interfaces, command definitions, priorities. Agnostic to runtime ECS.
Domain            ECS components, systems, factories, domain logic (mutable runtime state).
Application       Concrete command implementations.
Infrastructure    Registries, persistence, networking, scheduler, eventing, dispatcher, parser.
ConsoleApp        entry point, console I/O, game loop, game server.

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
