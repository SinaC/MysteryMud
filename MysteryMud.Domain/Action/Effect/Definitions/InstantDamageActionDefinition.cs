using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

public class InstantDamageActionDefinition : EffectActionDefinition
{
    public required CompiledFormula AmountCompiledFormula { get; init; }
    public required DamageKind Kind { get; init; }
}
