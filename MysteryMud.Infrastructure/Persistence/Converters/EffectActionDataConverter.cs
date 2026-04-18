using MysteryMud.Infrastructure.Persistence.Dto.Actions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MysteryMud.Infrastructure.Persistence.Converters;

internal class EffectActionDataConverter : JsonConverter<EffectActionData>
{
    public override EffectActionData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var type = root.GetProperty("Type").GetString();

        return type switch
        {
            "StatModifier" => JsonSerializer.Deserialize<CharacterStatModifierData>(root.GetRawText(), options), // TODO: depends on character or item
            "ResourceModifier" => JsonSerializer.Deserialize<ResourceModifierData>(root.GetRawText(), options),
            "RegenModifier" => JsonSerializer.Deserialize<RegenModifierData>(root.GetRawText(), options),
            "PeriodicHeal" => JsonSerializer.Deserialize<PeriodicHealData>(root.GetRawText(), options),
            "PeriodicDamage" => JsonSerializer.Deserialize<PeriodicDamageData>(root.GetRawText(), options),
            "InstantDamage" => JsonSerializer.Deserialize<InstantDamageData>(root.GetRawText(), options),
            "InstantHeal" => JsonSerializer.Deserialize<InstantHealData>(root.GetRawText(), options),
            "ApplyTag" => JsonSerializer.Deserialize<ApplyCharacterTagActionData>(root.GetRawText(), options),
            "ApplyItemTag" => JsonSerializer.Deserialize<ApplyItemTagActionData>(root.GetRawText(), options),
            _ => throw new NotSupportedException($"Unknown action type: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, EffectActionData value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}