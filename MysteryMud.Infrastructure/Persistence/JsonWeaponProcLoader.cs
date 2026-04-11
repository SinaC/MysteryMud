using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Action.Attack.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Dto;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonWeaponProcLoader
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public List<WeaponProcDefinition> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"WeaponProc JSON file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<WeaponProcData>>(json, _serializerOptions) ?? [];

        var weaponProcs = new List<WeaponProcDefinition>();
        foreach (var entry in data)
        {
            if (entry.Effects == null || entry.Effects.Count == 0)
                throw new Exception($"No effect found on weapon proc {entry.Name}");
            var effects = MapEffects(entry);
            var weaponProc = new WeaponProcDefinition
            {
                Id = entry.Name.ComputeUniqueId(),
                Name = entry.Name,
                Chance = entry.Chance, // TODO: formula
                EffectDefinitions = effects
            };
            weaponProcs.Add(weaponProc);
        }

        return weaponProcs;
    }

    private List<WeaponProcEffectDefinition> MapEffects(WeaponProcData data)
    {
        var definitions = new List<WeaponProcEffectDefinition>();
        foreach (var entry in data.Effects)
        {
            var definition = new WeaponProcEffectDefinition
            {
                EffectName = entry.Name,
                Target = EnumParser.Parse(entry.Target, WeaponProcTarget.Opponent)
            };
            definitions.Add(definition);
        }
        return definitions;
    }
}
