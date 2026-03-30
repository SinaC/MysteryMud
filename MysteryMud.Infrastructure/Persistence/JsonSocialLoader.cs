using MysteryMud.GameData.Definitions;
using MysteryMud.Infrastructure.Persistence.Dto;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonSocialLoader
{
    public List<SocialDefinition> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Command JSON file not found: {filePath}");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<SocialDefinitionData>>(json, options) ?? [];

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
