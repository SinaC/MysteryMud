using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public class EffectDefinition
{
    public string Id = default!;
    public EffectTagId Tag;
    public StackingRule Stacking;
    public int MaxStacks = 1;
    public AffectFlags Flags; // TODO
    public StatModifierDefinition[] StatModifiers = []; // formula ?
    public Func<World, Entity, Entity, int>? DurationFunc;
    public int TickRate; // in ticks (0: pure duration effect)
    public bool TickOnApply; // true: tick immediately
    public DotDefinition? Dot;
    public HotDefinition? Hot;
    public string? ApplyMessage; // TODO: on spell ?
    public string? WearOffMessage; // TODO: on spell ?
}
