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
            var command = Map(entry);
            commands.Add(command);
        }
        return commands;
    }

    internal static CommandDefinition Map(CommandDefinitionData data) // will be used by JsonAbilitLoader
        => new()
        {
            Id = data.Name.ComputeUniqueId(),
            Name = data.Name,
            Aliases = data.Aliases ?? [],
            CannotBeForced = data.CannotBeForced,
            RequiredLevel = Enum.Parse<CommandLevelKind>(data.RequiredLevel, ignoreCase: true),
            MinimumPosition = Enum.Parse<PositionKind>(data.MinimumPosition, ignoreCase: true),
            Priority = data.Priority,
            DisallowAbbreviation = data.DisallowAbbreviation,
            HelpText = data.HelpText,
            Syntaxes = data.Syntaxes,
            Categories = data.Categories,
            ThrottlingCategories = data.ThrottlingCategories.Aggregate(CommandThrottlingCategories.None, (accumulator, cat) => accumulator | Enum.Parse<CommandThrottlingCategories>(cat, ignoreCase: true)),
        };
}
