namespace TinyECS.UnitTests;

public class EntityIdTests
{
    // -------------------------------------------------------------------------
    // Construction & field extraction
    // -------------------------------------------------------------------------

    [Fact]
    public void Default_IsInvalid()
    {
        var id = default(EntityId);
        Assert.False(id.IsValid);
        Assert.Equal(EntityId.Invalid, id);
    }

    [Fact]
    public void InvalidSingleton_IsInvalid()
    {
        Assert.False(EntityId.Invalid.IsValid);
        Assert.Equal(0u, EntityId.Invalid.Index);
        Assert.Equal(0u, EntityId.Invalid.Generation);
    }

    [Theory]
    [InlineData(0u, 1u)]
    [InlineData(1u, 1u)]
    [InlineData(100u, 5u)]
    [InlineData(uint.MaxValue >> 1, 42u)]
    public void Constructor_RoundTrips_IndexAndGeneration(uint index, uint generation)
    {
        var id = new EntityId(index, generation);
        Assert.Equal(index, id.Index);
        Assert.Equal(generation, id.Generation);
    }

    [Theory]
    [InlineData(0u, 1u)]
    [InlineData(7u, 3u)]
    public void IsValid_TrueWhenGenerationNonZero(uint index, uint generation)
    {
        var id = new EntityId(index, generation);
        Assert.True(id.IsValid);
    }

    // -------------------------------------------------------------------------
    // Equality
    // -------------------------------------------------------------------------

    [Fact]
    public void SameIndexAndGeneration_AreEqual()
    {
        var a = new EntityId(5, 2);
        var b = new EntityId(5, 2);
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void SameIndex_DifferentGeneration_AreNotEqual()
    {
        var a = new EntityId(5, 1);
        var b = new EntityId(5, 2);
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void DifferentIndex_SameGeneration_AreNotEqual()
    {
        var a = new EntityId(1, 1);
        var b = new EntityId(2, 1);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetHashCode_EqualEntities_SameHash()
    {
        var a = new EntityId(10, 3);
        var b = new EntityId(10, 3);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_BoxedObject_Works()
    {
        var a = new EntityId(1, 1);
        object b = new EntityId(1, 1);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var a = new EntityId(1, 1);
        Assert.False(a.Equals("not an entity"));
    }

    // -------------------------------------------------------------------------
    // ToString
    // -------------------------------------------------------------------------

    [Fact]
    public void ToString_ContainsIndexAndGeneration()
    {
        var id = new EntityId(7, 3);
        var s = id.ToString();
        Assert.Contains("7", s);
        Assert.Contains("3", s);
    }

    // -------------------------------------------------------------------------
    // Bit layout — generation lives in high 32 bits
    // -------------------------------------------------------------------------

    [Fact]
    public void Generation_DoesNotAliasIndex()
    {
        // Index=1,Gen=2 must differ from Index=2,Gen=1
        var a = new EntityId(1, 2);
        var b = new EntityId(2, 1);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MaxIndex_NoOverflowIntoGeneration()
    {
        // Use a large index — shouldn't corrupt the generation field
        uint bigIndex = 0x0FFF_FFFF;
        var id = new EntityId(bigIndex, 1);
        Assert.Equal(bigIndex, id.Index);
        Assert.Equal(1u, id.Generation);
    }
}