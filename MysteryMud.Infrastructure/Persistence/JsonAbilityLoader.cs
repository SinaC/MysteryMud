using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Ability.Definitions;
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

    public List<AbilityDefinition> Load(string filePath)
    {
        if(!File.Exists(filePath))
            throw new FileNotFoundException($"Ability JSON file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<AbilityDefinitionData>>(json, _serializerOptions) ?? [];

        var abilities = new List<AbilityDefinition>();
        foreach (var entry in data)
        {
            if (entry.Effects == null || entry.Effects.Count == 0)
                throw new Exception($"No effect found on ability {entry.Name}");
            var ability = new AbilityDefinition
            {
                Id = entry.Name.ComputeUniqueId(),
                Name = entry.Name,
                Kind = Enum.Parse<AbilityKind>(entry.Kind, ignoreCase: true),
                CastTime = entry.CastTime,
                Cooldown = entry.Cooldown,
                ResourceCost = entry.ResourceCost,
                Effects = entry.Effects,
            };
            abilities.Add(ability);
        }
        return abilities;
    }
}
