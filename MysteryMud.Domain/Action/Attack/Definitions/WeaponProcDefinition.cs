namespace MysteryMud.Domain.Action.Attack.Definitions;

public struct WeaponProcDefinition
{
    public int Id;
    public string Name;

    public int Chance; // TODO: formula
    // TODO: message ?

    public List<WeaponProcEffectDefinition> EffectDefinitions;
}
