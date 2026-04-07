using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Effect;

public class EffectRuntime
{
    public required int Id { get; init; }
    public required string Name { get; init; }

    // state-effect
    public Func<EffectContext, decimal>? DurationFunc { get; init; } // if null -> stateless effect
    public required EffectTagId Tag { get; init; }
    public required StackingRule Stacking { get; init; }
    public required int MaxStacks { get; init; } = 1;
    public required bool TickOnApply { get; init; } = false; // if true, tick actions are triggered immediately
    public required int TickRate { get; init; } = 0; // in ticks (0: pure duration effect if DurationFunc is not null)

    // effect delegate
    public Action<EffectContext>[] OnApply { get; init; } = [];
    public Action<EffectContext>[] OnTick { get; init; } = [];
    public Action<EffectContext>[] OnExpire { get; init; } = [];
    //TODO: public Action<EffectContext, DamageAction>[] OnReceiveDamage { get; init; } = [];
}
