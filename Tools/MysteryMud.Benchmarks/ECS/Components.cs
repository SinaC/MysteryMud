namespace MysteryMud.Benchmarks.ECS;

// =============================================================================
// Shared component definitions.
//
// 30 structs — enough to give the benchmark realistic breadth.
// All are value types so both ECS implementations store them by value.
// We use a mix of data-bearing (Position, Velocity, Health, …) and pure-tag
// (CombatState, Dead, Stunned, …) components to mirror a real MUD workload.
//
// These live in the global namespace so they're accessible from both the
// custom Ecs namespace and from Arch (which uses the CLR type directly).
// =============================================================================

// ── Data components ──────────────────────────────────────────────────────────
public struct Position { public float X, Y, Z; }
public struct Velocity { public float Dx, Dy, Dz; }
public struct Health { public int Current, Max; }
public struct Mana { public int Current, Max; }
public struct Stamina { public int Current, Max; }
public struct Level { public int Value; }
public struct Experience { public long Points; }
public struct Armor { public int Rating; }
public struct Damage { public int Min, Max; }
public struct Speed { public float Value; }
public struct Gold { public int Amount; }
public struct Weight { public float Value; }
public struct Age { public int Ticks; }
public struct RoomRef { public int Vnum; }
public struct TargetRef { public ulong EntityPacked; }  // packed EntityId value

// ── Buff / debuff data ────────────────────────────────────────────────────────
public struct PoisonDebuff { public int DamagePerTick, TicksRemaining; }
public struct BlindDebuff { public int TicksRemaining; }
public struct SilenceDebuff { public int TicksRemaining; }
public struct HasteDebuff { public int TicksRemaining; }   // "haste" can be buff or debuff
public struct RegenBuff { public int HpPerTick, TicksRemaining; }
public struct ShieldBuff { public int Absorb, TicksRemaining; }
public struct BerserkBuff { public int BonusDamage, TicksRemaining; }

// ── Tag components (zero-size — presence is the data) ────────────────────────
public struct CombatState { }
public struct Dead { }
public struct Stunned { }
public struct Invisible { }
public struct Sanctuary { }
public struct Sleeping { }
public struct Fleeing { }
public struct PlayerTag { }   // marks player-controlled entities
public struct MobileTag { }   // marks NPCs / mobs
