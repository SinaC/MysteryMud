using MysteryMud.Infrastructure.Persistence.Dto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MysteryMud.Infrastructure.Persistence.Converters;

internal class ContextualizedMessageConverter : JsonConverter<ContextualizedMessageData>
{
    public override ContextualizedMessageData Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // "OnNoTarget": "Bash whom?"  →  ToActor only
        if (reader.TokenType == JsonTokenType.String)
            return new ContextualizedMessageData(reader.GetString()!);

        // "OnSuccess": { "ToActor": "...", "ToRoom": "..." }
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(
                $"Expected string or object for ContextualizedMessageData, got {reader.TokenType}.");

        string? toActor = null, toTarget = null, toRoom = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name.");

            var propName = reader.GetString();
            reader.Read();

            switch (propName)
            {
                case "ToActor": toActor = reader.GetString(); break;
                case "ToTarget": toTarget = reader.GetString(); break;
                case "ToRoom": toRoom = reader.GetString(); break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return new ContextualizedMessageData(toActor, toTarget, toRoom);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ContextualizedMessageData value,
        JsonSerializerOptions options)
    {
        // If only ToActor is set, write the compact string form back out
        if (value.ToTarget is null && value.ToRoom is null)
        {
            writer.WriteStringValue(value.ToActor);
            return;
        }

        writer.WriteStartObject();
        if (value.ToActor is not null) writer.WriteString("ToActor", value.ToActor);
        if (value.ToTarget is not null) writer.WriteString("ToTarget", value.ToTarget);
        if (value.ToRoom is not null) writer.WriteString("ToRoom", value.ToRoom);
        writer.WriteEndObject();
    }
}
