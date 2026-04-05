using MysteryMud.Core.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Dto;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonCommandLoader
{
    private static readonly JsonSerializerOptions _serializerOptions = new ()
    {
        PropertyNameCaseInsensitive = true
    };

    // TODO: load from file or string instead of passing in a pre-parsed DTO
    public List<CommandDefinition> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Command JSON file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<CommandDefinitionData>>(json, _serializerOptions) ?? [];

        var commands = new List<CommandDefinition>();
        foreach (var entry in data)
        {
            var command = new CommandDefinition
            {
                Id = entry.Name.ComputeUniqueId(),
                Name = entry.Name,
                Aliases = entry.Aliases,
                CannotBeForced = entry.CannotBeForced,
                RequiredLevel = Enum.Parse<CommandLevelKind>(entry.RequiredLevel, ignoreCase: true),
                MinimumPosition = Enum.Parse<PositionKind>(entry.MinimumPosition, ignoreCase: true),
                Priority = entry.Priority,
                AllowAbbreviation = entry.AllowAbbreviation,
                HelpText = entry.HelpText,
                Syntaxes = entry.Syntaxes,
                Categories = entry.Categories,
                ThrottlingCategories = entry.ThrottlingCategories.Aggregate(CommandThrottlingCategories.None, (accumulator, cat) => accumulator | Enum.Parse<CommandThrottlingCategories>(cat, ignoreCase: true)),
            };
            commands.Add(command);
        }
        return commands;
    }
}
