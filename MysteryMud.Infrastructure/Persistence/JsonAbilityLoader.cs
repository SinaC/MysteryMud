using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.GameData.Definitions;
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

            var kind = Enum.Parse<AbilityKind>(entry.Kind, ignoreCase: true);
            CommandDefinition? command = entry.Command == null
                ? null
                : JsonCommandLoader.Map(entry.Command);
            if (kind == AbilityKind.Skill && command is null)
                throw new Exception($"Skill ability {entry.Name} must declare a command");

            var costs = entry.Costs == null || entry.Costs.Count == 0
                ? []
                : entry.Costs.Select(MapResourceCost).ToList();

            var ability = new AbilityDefinition
            {
                Id = entry.Name.ComputeUniqueId(),
                Name = entry.Name,
                Kind = kind,
                CastTime = entry.CastTime,
                Cooldown = entry.Cooldown,
                Costs = costs,
                Effects = entry.Effects,
                Command = command,
            };
            abilities.Add(ability);
        }
        return abilities;
    }

    private ResourceCost MapResourceCost(ResourceCostData data)
        => new()
        {
            Kind = Enum.Parse<ResourceKind>(data.Kind, ignoreCase: true),
            Amount = data.Amount,
        };
}
