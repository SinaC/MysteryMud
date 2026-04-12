namespace MysteryMud.Core.Effects;

public struct DamageResult
{
    public bool IsSuccess;
    public decimal Amount;
    public bool Killed;
    public int EffectiveAmount; // after mitigation, clamp, etc.
}
