using MysteryMud.Infrastructure.Persistence.Dto.Rules;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MysteryMud.Infrastructure.Persistence.Converters;

internal class AbilityValidationRuleDataConverter : JsonConverter<AbilityValidationRuleData>
{
    public override AbilityValidationRuleData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var type = root.GetProperty("Type").GetString();

        return type switch
        {
            "AffectedBy" => JsonSerializer.Deserialize<AffectedByRuleData>(root.GetRawText(), options), // TODO: depends on character or item
            "NotAffectedBy" => JsonSerializer.Deserialize<NotAffectedByRuleData>(root.GetRawText(), options), // TODO: depends on character or item
            "HasWeaponType" => JsonSerializer.Deserialize<HasWeaponTypeRuleData>(root.GetRawText(), options),
            "NotFighting" => JsonSerializer.Deserialize<NotFightingRuleData>(root.GetRawText(), options),
            "SavesSpell" => JsonSerializer.Deserialize<SavesSpellRuleData>(root.GetRawText(), options),
            _ => throw new NotSupportedException($"Unknown rule type: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, AbilityValidationRuleData value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}