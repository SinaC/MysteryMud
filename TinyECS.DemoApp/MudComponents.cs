namespace TinyECS.DemoApp;

// =============================================================================
// TAG COMPONENTS  (zero or near-zero data — presence IS the information)
// =============================================================================

/// <summary>EntityId is currently in a combat round.</summary>
public struct CombatState { }

/// <summary>EntityId is channelling a spell.  Cleared when cast finishes or is interrupted.</summary>
public struct Casting
{
    public int SpellId;
    public int TicksRemaining;
    public EntityId Target;       // EntityId.Invalid for self-cast / area
}

/// <summary>EntityId cannot act this tick (bash, stun, etc.).</summary>
public struct Stunned
{
    public int TicksRemaining;
}

/// <summary>EntityId is asleep (sleep spell, etc.).</summary>
public struct Sleeping { }

/// <summary>EntityId is invisible.</summary>
public struct Invisible { }

/// <summary>EntityId is sanctuary-buffed (half physical damage).</summary>
public struct Sanctuary { }

/// <summary>EntityId is poisoned.</summary>
public struct Poisoned
{
    public int DamagePerTick;
    public int TicksRemaining;
}

/// <summary>EntityId is dead and pending extraction this tick.</summary>
public struct Dead { }

/// <summary>Marks a player-controlled entity.</summary>
public struct PlayerControlled { }

/// <summary>Marks a mobile (NPC).</summary>
public struct Mobile { }

// =============================================================================
// DATA COMPONENTS  (meaningful fields)
// =============================================================================

public struct Health
{
    public int Current;
    public int Max;
}

public struct Mana
{
    public int Current;
    public int Max;
}

public struct Move
{
    public int Current;
    public int Max;
}

public struct Level
{
    public int Value;
}

public struct Position
{
    /// <summary>Vnum of the room the EntityId is in.</summary>
    public int RoomVnum;
}

public struct Stats
{
    public int Str, Int, Wis, Dex, Con, Cha;
}

// =============================================================================
// RELATION COMPONENTS  (EntityId → entity)
//
// Relations are regular components whose value is another EntityId.
// They satisfy "who is this EntityId related to?" without a separate graph.
// Always validate the target with world.IsAlive(relation.Target) before use.
// =============================================================================

/// <summary>This EntityId is actively fighting Target.</summary>
public struct Fighting
{
    public EntityId Target;
}

/// <summary>This EntityId follows Leader (group mechanic).</summary>
public struct Following
{
    public EntityId Leader;
}

/// <summary>This EntityId is charmed by Charmer and will obey its commands.</summary>
public struct Charmed
{
    public EntityId Charmer;
}

/// <summary>
/// Inverse relation: Target is being hunted by this entity.
/// Useful for mobs with the SENTINEL / AGGRESSIVE flags.
/// </summary>
public struct Hunting
{
    public EntityId Target;
}
