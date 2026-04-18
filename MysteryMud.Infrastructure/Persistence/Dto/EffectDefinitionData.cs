using MysteryMud.Infrastructure.Persistence.Dto.Actions;

namespace MysteryMud.Infrastructure.Persistence.Dto;

internal record EffectDefinitionData
(
    string Name,
    string Tag,
    string TagKind,
    bool? IsHarmful, // if harmful status cannot be infered from actions
    string Stacking,
    int MaxStacks,
    string DurationFormula,
    int TickRate, // in ticks (0: pure duration effect)
    bool TickOnApply, // true: tick immediately
    ContextualizedMessageData ApplyMessage,
    ContextualizedMessageData WearOffMessage,
    List<EffectActionData> Actions
);
