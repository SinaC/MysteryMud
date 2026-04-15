using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

[EffectActionTarget(EffectTargetKind.Character)]
public class InstantHealActionDefinition : EffectActionDefinition
{
    public required EffectCompiledFormula AmountCompiledFormula { get; init; }
}
