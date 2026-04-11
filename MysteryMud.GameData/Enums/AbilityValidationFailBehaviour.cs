namespace MysteryMud.GameData.Enums;

public enum AbilityValidationFailBehaviour
{
    // Abort the entire ability (same as a source rule failure).
    Abort,

    // Silently skip this target and continue.
    Skip,

    // Skip this target and send the keyed message to the caster.
    SkipWithMessage,
}
