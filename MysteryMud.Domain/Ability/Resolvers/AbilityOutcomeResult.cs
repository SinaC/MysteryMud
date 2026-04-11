namespace MysteryMud.Domain.Ability.Resolvers;

public struct AbilityOutcomeResult
{
    public bool Success;
    public string Outcome; // "OnSuccess" / "OnFailure"
    public List<int> EffectIdsToApply;
}