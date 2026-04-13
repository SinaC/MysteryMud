using MysteryMud.Domain.Action.Attack.Definitions;

namespace MysteryMud.Domain.Action.Attack;

public interface IWeaponProcRegistry
{
    void Register(WeaponProcDefinition definition);
    void Register(IEnumerable<WeaponProcDefinition> definitions);

    bool TryGetRuntime(string name, out WeaponProcRuntime? proc);
    bool TryGetRuntime(int id, out WeaponProcRuntime? proc);
}
