using Arch.Core;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Data;

public class EffectTemplate
{
    public string Name;
    public EffectTagId Tag;
    public StackingRule Stacking;
    public int MaxStacks = 1;
    public AffectFlags Flags; // TODO
    public StatModifierDefinition[] StatModifiers = []; // formula ?
    public Func<Entity, Entity, int> DurationFunc;
    public Func<Entity, Entity, DotDefinition?> DotFunc;
    public Func<Entity, Entity, HotDefinition?> HotFunc;
    public string ApplyMessage; // TODO: on spell ?
    public string WearOffMessage; // TODO: on spell ?
}
