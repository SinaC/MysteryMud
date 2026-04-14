using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class InstantDamageActionDefinition : EffectActionDefinition
{
    public required EffectCompiledFormula AmountCompiledFormula { get; init; }
    public required DamageKind Kind { get; init; }
}
