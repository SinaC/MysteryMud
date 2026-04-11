using MysteryMud.GameData.Definitions;
using MysteryMud.Infrastructure.Persistence.Dto;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonSocialLoader
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public List<SocialDefinition> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Command JSON file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<SocialDefinitionData>>(json, _serializerOptions) ?? [];

        var socials = new List<SocialDefinition>();
        foreach (var s in data)
        {
            var command = new SocialDefinition
            {
                Name = s.Name,
                CharacterNoArg = s.CharacterNoArg,
                OthersNoArg = s.OthersNoArg,
                CharacterFound = s.CharacterFound,
                OthersFound = s.OthersFound,
                VictimFound = s.VictimFound,
                CharacterNotFound = s.CharacterNotFound,
                CharacterAuto = s.CharacterAuto,
                OthersAuto = s.OthersAuto
            };
            socials.Add(command);
        }
        return socials;
    }
}
