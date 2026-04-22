using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Definitions;

[EffectActionTarget(EffectTargetKind.Character)]
public class InstantRestoreResourceActionDefinition : EffectActionDefinition
{
    public required ResourceKind Resource { get; set; }
    public required EffectCompiledFormula AmountCompiledFormula { get; init; }
}
