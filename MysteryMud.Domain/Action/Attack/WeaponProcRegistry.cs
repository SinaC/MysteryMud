using MysteryMud.Domain.Action.Attack.Definitions;
using MysteryMud.Domain.Action.Attack.Factories;
using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Action.Attack;

public class WeaponProcRegistry : IWeaponProcRegistry
{
    private readonly Dictionary<string, WeaponProcRuntime> _procByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, WeaponProcRuntime> _procById = [];

    private readonly IEffectRegistry _effectRegistry;

    public WeaponProcRegistry(IEffectRegistry effectRegistry)
    {
        _effectRegistry = effectRegistry;
    }

    public void Register(WeaponProcDefinition definition)
    {
        var weaponProcRuntime = WeaponProcRuntimeFactory.Create(_effectRegistry, definition);

        _procByName.Add(weaponProcRuntime.Name, weaponProcRuntime);
        _procById.Add(weaponProcRuntime.Id, weaponProcRuntime);
    }

    public void Register(IEnumerable<WeaponProcDefinition> definitions)
    {
        foreach(var definition in definitions)
            Register(definition);
    }

    public bool TryGetRuntime(string name, out WeaponProcRuntime? proc)
        => _procByName.TryGetValue(name, out proc);

    public bool TryGetRuntime(int id, out WeaponProcRuntime? proc)
        => _procById.TryGetValue(id, out proc);
}
