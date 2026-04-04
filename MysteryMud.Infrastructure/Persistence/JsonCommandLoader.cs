using MysteryMud.Core.Extensions;
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
        foreach (var cmd in data)
        {
            var command = new CommandDefinition
            {
                Id = cmd.Name.ComputeUniqueId(),
                Name = cmd.Name,
                Aliases = cmd.Aliases,
                CannotBeForced = cmd.CannotBeForced,
                RequiredLevel = Enum.Parse<CommandLevelKind>(cmd.RequiredLevel, ignoreCase: true),
                MinimumPosition = Enum.Parse<PositionKind>(cmd.MinimumPosition, ignoreCase: true),
                Priority = cmd.Priority,
                AllowAbbreviation = cmd.AllowAbbreviation,
                HelpText = cmd.HelpText,
                Syntaxes = cmd.Syntaxes,
                Categories = cmd.Categories,
                ThrottlingCategories = cmd.ThrottlingCategories.Aggregate(CommandThrottlingCategories.None, (accumulator, cat) => accumulator | Enum.Parse<CommandThrottlingCategories>(cat, ignoreCase: true)),
            };
            commands.Add(command);
        }
        return commands;
    }
}
