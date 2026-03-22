using Arch.Core;
using MysteryMud.ConsoleApp3.Extensions;

namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public struct ArgValue
{
    public enum ArgType
    {
        Failed,
        Raw,
        Int,
        String,
        Item,
        Entity,
        EntityCollection
    }

    public ArgType Type;
    public ReadOnlyMemory<char> RawInput; // raw token, used for deferred resolution
    public ItemArg ItemValue;
    public List<Entity> Entities;
    public Entity EntityValue;
    public int IntValue;
    public string StringValue;

    // factories
    public static ArgValue String(ReadOnlySpan<char> span) => new ArgValue
    {
        Type = ArgType.String,
        RawInput = span.ToArray(),
        StringValue = span.ToString()
    };

    public static ArgValue Raw(ReadOnlySpan<char> span) => new ArgValue
    {
        Type = ArgType.Raw,
        RawInput = span.ToArray()
    };

    public static ArgValue Failed(ReadOnlySpan<char> span) => new ArgValue
    {
        Type = ArgType.Failed,
        RawInput = span.ToArray()
    };

    public override string ToString()
    {
        switch (Type)
        {
            case ArgType.Raw: return $"Raw({RawInput})";
            case ArgType.Int: return $"Int({IntValue})";
            case ArgType.String: return $"String({StringValue})";
            case ArgType.Item: return $"Item({ItemValue})[{EntityValue.DebugName}]";
            case ArgType.Entity: return $"Entity[{EntityValue.DebugName}]";
            case ArgType.EntityCollection: return $"EntityCollection[{string.Join(',', Entities.Select(x => x.DebugName))}]";
        }
        return "???";
    }
}
