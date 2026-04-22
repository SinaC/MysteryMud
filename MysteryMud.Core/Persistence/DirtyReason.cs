namespace MysteryMud.Core.Persistence;

// ─────────────────────────────────────────────────────────────
//  Why an entity is dirty
// ─────────────────────────────────────────────────────────────

[Flags]
public enum DirtyReason : uint
{
    None = 0,

    // ── Deferrable — safe to batch, flush on autosave ────────
    CoreData = 1 << 0,
    Stats = 1 << 1,
    Resources = 1 << 2,
    AbilityProgress = 1 << 3,
    AbilityMastery = 1 << 4,
    AbilityCooldown = 1 << 5,
    Effects = 1 << 6,
    ItemEquipped = 1 << 7,
    ItemRemoved = 1 << 8,
    ItemGained = 1 << 9,
    ItemLost = 1 << 10,
    Experience = 1 << 11,

    // ── Critical — flush at end of current tick ──────────────
    AbilityGained = 1 << 20,
    LevelUp = 1 << 21,
    Death = 1 << 22,
    Respawn = 1 << 23,

    // ── Composites ───────────────────────────────────────────
    Deferrable = CoreData | Stats | Resources | AbilityProgress
                    | AbilityMastery | AbilityCooldown | Effects | ItemEquipped
                    | ItemGained | ItemLost | Experience,

    Critical = LevelUp | Death | Respawn,

    All = uint.MaxValue
}
