using Arch.Core;

namespace MysteryMud.GameData.Definitions;

public readonly struct HotDefinition
{
    public required Func<World, Entity, Entity, int> HealFunc { get; init; }
}
