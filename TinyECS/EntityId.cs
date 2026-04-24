namespace TinyECS;

/// <summary>
/// An EntityId is a 32-bit index + 32-bit generation packed into a ulong.
/// The generation is bumped every time a slot is recycled, so stale
/// EntityIds obtained before a Destroy() are detectable and rejected.
/// </summary>
public readonly struct EntityId : IEquatable<EntityId>
{
    public static readonly EntityId Invalid = default;

    // High 32 bits = generation, low 32 bits = index
    private readonly ulong _value;

    public uint Index => (uint)(_value & 0xFFFF_FFFF);
    public uint Generation => (uint)(_value >> 32);

    public bool IsValid => _value != 0;

    internal EntityId(uint index, uint generation)
    {
        // generation 0 is reserved for "invalid", so real generations start at 1
        _value = ((ulong)generation << 32) | index;
    }

    public bool Equals(EntityId other) => _value == other._value;
    public override bool Equals(object? obj) => obj is EntityId e && Equals(e);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => $"Entity({Index}:g{Generation})";

    public static bool operator ==(EntityId a, EntityId b) => a._value == b._value;
    public static bool operator !=(EntityId a, EntityId b) => a._value != b._value;
}
