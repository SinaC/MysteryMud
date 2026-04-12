namespace MysteryMud.Core.Effects;

public struct HealResult
{
    public bool IsSuccess;
    public decimal Amount;
    public bool MaxHealth;
    public int EffectiveAmount;
}
