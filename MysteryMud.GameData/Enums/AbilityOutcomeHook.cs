namespace MysteryMud.GameData.Enums;

public enum AbilityOutcomeHook
{
    // Evaluated during execution. Selects Success or Failure effect list.
    // Ability always fires; costs always paid. Typical for skills.
    Execution,

    // Evaluated during validation. Failure aborts execution but costs are
    // still paid (the attempt was made). Typical for spells.
    Validation,
}
