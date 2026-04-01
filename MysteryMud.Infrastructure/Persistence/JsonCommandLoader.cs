using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Dto;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonCommandLoader
{
    // TODO: load from file or string instead of passing in a pre-parsed DTO
    public List<CommandDefinition> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Command JSON file not found: {filePath}");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<CommandDefinitionData>>(json, options) ?? [];

        var commands = new List<CommandDefinition>();
        foreach (var c in data)
        {
            var command = new CommandDefinition
            {
                Name = c.Name,
                Aliases = c.Aliases,
                CannotBeForced = c.CannotBeForced,
                RequiredLevel = Enum.Parse<CommandLevelKind>(c.RequiredLevel, ignoreCase: true),
                MinimumPosition = Enum.Parse<PositionKind>(c.MinimumPosition, ignoreCase: true),
                Priority = c.Priority,
                AllowAbbreviation = c.AllowAbbreviation,
                HelpText = c.HelpText,
                Syntaxes = c.Syntaxes,
                Categories = c.Categories
            };
            commands.Add(command);
        }
        return commands;
    }
}
