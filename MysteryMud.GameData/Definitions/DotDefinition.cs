using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public readonly struct DotDefinition
{
    public required Func<World, Entity, Entity, int> DamageFunc { get; init; }
    public required DamageTypes DamageType { get; init; }
    public required int TickRate { get; init; }
}
