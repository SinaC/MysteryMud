using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Items;

public struct Weapon
{
    public WeaponKind Kind;
    public int DiceCount;
    public int DiceValue;

    public int? ProcEffectId; // TODO: list (id + chance)
}
