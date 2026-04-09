namespace MysteryMud.Domain.Ability.Resolvers;

public struct AbilityExecutionResult
{
    public bool Success;
    public string Outcome; // "OnSuccess" / "OnFailure"
    public List<int> EffectIdsToApply;
}