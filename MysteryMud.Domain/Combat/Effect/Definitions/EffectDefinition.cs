using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Effect.Definitions;

public class EffectDefinition
{
    public required int Id { get; init; }
    public required string Name { get; init; }

    // state-effect
    public Func<EffectContext, decimal>? DurationFunc { get; init; } // if null -> stateless effect
    public EffectTagId Tag { get; init; }
    public StackingRule Stacking { get; init; }
    public int MaxStacks { get; init; } = 1;
    public bool TickOnApply { get; init; } = false; // if true, tick actions are triggered immediately
    public int TickRate { get; init; } = 0; // in ticks (0: pure duration effect if DurationFunc is not null)

    //
    public string WearOffMessage { get; init; } = default!;
    public string ApplyMessage { get; init; } = default!;

    // actions
    public required List<EffectActionDefinition> Actions { get; init; } = [];
}
