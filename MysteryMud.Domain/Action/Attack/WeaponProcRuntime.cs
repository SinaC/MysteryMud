using MysteryMud.Domain.Action.Attack.Definitions;

namespace MysteryMud.Domain.Action.Attack;

public class WeaponProcRuntime
{
    public int Id; // generated
    public string Name = default!;

    public int Chance;
    // TODO: message ?

    public List<WeaponProcEffectRuntime> WeaponProcEffectRuntimes = [];
}
