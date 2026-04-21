namespace MysteryMud.Core.Persistence;

// ─────────────────────────────────────────────────────────────
//  Why an entity is dirty
// ─────────────────────────────────────────────────────────────

[Flags]
public enum DirtyReason : uint
{
    None = 0,

    // Core character
    CoreData = 1 << 0,   // level, location, position, form
    Stats = 1 << 1,
    Resources = 1 << 2,

    // Progression
    Experience = 1 << 3,

    // Abilities
    AbilityGained = 1 << 4,
    AbilityProgress = 1 << 5,
    AbilityMastery = 1 << 6,
    AbilityCooldown = 1 << 7,   // low-priority; flushed on autosave/disconnect only

    // Effects
    Effects = 1 << 8,

    // Items
    ItemGained = 1 << 9,
    ItemLost = 1 << 10,
    ItemEquipped = 1 << 11,
    ItemRemoved = 1 << 12,

    // Convenience composites
    AnyAbility = AbilityGained | AbilityProgress | AbilityMastery | AbilityCooldown,
    AnyItem = ItemGained | ItemLost | ItemEquipped | ItemRemoved,
    All = uint.MaxValue
}
