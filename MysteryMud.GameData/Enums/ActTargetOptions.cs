namespace MysteryMud.GameData.Enums;

public enum ActTargetOptions
{
    /// <summary>
    /// Everyone in the room except actor
    /// </summary>
    ToRoom,
    /// <summary>
    /// Only to Character
    /// </summary>
    ToCharacter,
    /// <summary>
    /// Everyone in the room
    /// </summary>
    ToAll,
    /// <summary>
    /// Everyone in the group
    /// </summary>
    ToGroup,
    /// <summary>
    /// Everyone in the room except actor and victims
    ToNotVictims
}
