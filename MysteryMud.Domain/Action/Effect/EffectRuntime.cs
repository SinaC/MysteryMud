using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect;

public class EffectRuntime
{
    public int Id;
    public string Name = default!;

    // state-effect
    public Func<EffectContext, decimal>? DurationFunc; // if null -> stateless effect
    public CharacterEffectTagId Tag = CharacterEffectTagId.None; // TODO: what about ItemEffectTagId ?
    public StackingRule Stacking = StackingRule.None;
    public int MaxStacks = 1;
    public bool TickOnApply = false; // if true, tick actions are triggered immediately
    public int TickRate = 0; // in ticks (0: pure duration effect if DurationFunc is not null)

    // effect delegate
    public Action<EffectExecutionContext>[] OnApply = [];
    public Action<EffectExecutionContext>[] OnTick = [];
    public Action<EffectExecutionContext>[] OnExpire = [];
    //TODO: public Action<EffectContext, DamageAction>[] OnReceiveDamage = [];
}
