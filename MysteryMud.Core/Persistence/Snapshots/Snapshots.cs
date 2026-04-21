namespace MysteryMud.Core.Persistence.Snapshots;

// ─────────────────────────────────────────────────────────────
//  Primitive value records
// ─────────────────────────────────────────────────────────────

public record StatSnapshot(
    string Stat,
    int BaseValue,
    int EffValue);

public record ResourceSnapshot(
    string Resource,
    int Current,
    int Maximum,
    float BaseRegen,
    float CurrentRegen);

/// <summary>
/// All tick fields are stored as offsets relative to the tick counter
/// at save time. On load they are rebased to the current tick.
/// ExpirationRemaining == -1 means the effect is permanent.
/// </summary>
public record EffectSnapshot(
    string EffectId,
    string[] Tags,
    long TickRate,
    long NextTickOffset,
    long ExpirationRemaining,   // -1 = permanent
    string? ParamsJson);

public record AbilitySnapshot(
    string AbilityKey,
    string ClassKey,
    int LearnedPercent,
    int LearnedLevel,
    int MasteryTier,
    long? CooldownTicksRemaining, // null = not on cooldown
    int? Charges);               // null = not charge-based

// ─────────────────────────────────────────────────────────────
//  Item snapshot
// ─────────────────────────────────────────────────────────────

public record ItemSnapshot(
    long Id,            // DB id, 0 for new items
    int TemplateVnum,
    string? EquippedSlot,
    long? ContainerItemId,
    string? ParamsJson,
    EffectSnapshot[] Effects);

// ─────────────────────────────────────────────────────────────
//  Player snapshot  (root aggregate)
// ─────────────────────────────────────────────────────────────

public record PlayerSnapshot(
    long Id,             // DB id, 0 for new players
    string Name,
    int Level,
    string LocationKey,    // "zone_id::room_vnum"
    string Position,       // enum name
    string Form,           // enum name
    long TotalXp,
    int AutoBehavior,   // bitmask
    string? OptionalJson,   // Gender, RespawnState, etc.
    StatSnapshot[] Stats,
    ResourceSnapshot[] Resources,
    EffectSnapshot[] Effects,
    AbilitySnapshot[] Abilities,
    ItemSnapshot[] Items);
