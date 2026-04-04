namespace MysteryMud.Infrastructure.Persistence.Dto.Effects;

public record EffectDefinitionData
(
    string Name,
    string Tag,
    string Stacking,
    int MaxStacks,
    string DurationFormula,
    int TickRate, // in ticks (0: pure duration effect)
    bool TickOnApply, // true: tick immediately
    string ApplyMessage,
    string WearOffMessage,
    List<EffectActionData> Actions
);
