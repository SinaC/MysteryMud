using MysteryMud.Infrastructure.Persistence.Converters;
using System.Text.Json.Serialization;

namespace MysteryMud.Infrastructure.Persistence.Dto;

/// <summary>
/// A message that can be sent to up to three audiences simultaneously.
/// Serializes as either a plain string (ToActor only) or a full object.
/// </summary>
[JsonConverter(typeof(ContextualizedMessageConverter))]
public sealed class ContextualizedMessageData
{
    /// <summary>Sent to the entity performing the action.</summary>
    public string? ToActor { get; init; }

    /// <summary>Sent to the entity receiving the action.</summary>
    public string? ToTarget { get; init; }

    /// <summary>Sent to everyone else in the room.</summary>
    public string? ToRoom { get; init; }

    /// <summary>Convenience constructor for actor-only messages.</summary>
    public ContextualizedMessageData(string toActor) => ToActor = toActor;

    public ContextualizedMessageData(string? toActor, string? toTarget, string? toRoom)
    {
        ToActor = toActor;
        ToTarget = toTarget;
        ToRoom = toRoom;
    }
}
