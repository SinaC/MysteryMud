using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Ability;
using MysteryMud.Domain.Effect;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Dto;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonAbilityLoader
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private EffectRegistry _effectRegistry;

    public JsonAbilityLoader(EffectRegistry effectRegistry)
    {
        _effectRegistry = effectRegistry;
    }

    public List<AbilityRuntime> Load(string filePath)
    {
        if(!File.Exists(filePath))
            throw new FileNotFoundException($"Ability JSON file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<AbilityDefinitionData>>(json, _serializerOptions) ?? [];

        var abilities = new List<AbilityRuntime>();
        foreach (var entry in data)
        {
            if (entry.Effects == null || entry.Effects.Count == 0)
                throw new Exception($"No effect found on ability {entry.Name}");
            var effectIds = new List<int>();
            foreach (var effectName in entry.Effects)
            {
                if (!_effectRegistry.TryGetValue(effectName, out var effectRuntime) || effectRuntime == null)
                    throw new Exception($"Unknown effect {effectName} found on ability {entry.Name}");
                effectIds.Add(effectRuntime.Id);
            }
            var ability = new AbilityRuntime
            {
                Id = entry.Name.ComputeUniqueId(),
                Name = entry.Name,
                Kind = Enum.Parse<AbilityKind>(entry.Kind, ignoreCase: true),
                CastTime = entry.CastTime,
                Cooldown = entry.Cooldown,
                ResourceCost = entry.ResourceCost,
                EffectIds = effectIds,
            };
            abilities.Add(ability);
        }
        return abilities;
    }
}
