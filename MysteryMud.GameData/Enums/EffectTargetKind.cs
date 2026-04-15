namespace MysteryMud.GameData.Enums;

[Flags]
public enum EffectTargetKind
{
    Character = 1 << 0,
    Item = 1 << 1,
    // Room    = 1 << 2,  // future
}
