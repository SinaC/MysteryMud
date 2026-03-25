namespace MysteryMud.Infrastructure.Persistence.Dto;

public record EffectTemplateData(
    string Name,
    string Tag,
    string Stacking,
    int MaxStacks,
    string[] Flags,
    StatModifierDefinitionData[] StatModifiers,
    string DurationFormula,
    DotData Dot,
    HotData Hot,
    string ApplyMessage,
    string WearOffMessage
);
