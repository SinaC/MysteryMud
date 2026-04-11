using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Items;

public struct Weapon
{
    public WeaponKind Kind;
    public int DiceCount;
    public int DiceValue;

    public List<int> ProcIds;
}
