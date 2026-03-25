namespace MysteryMud.Infrastructure.Persistence.Dto;

public record SpellRootData(
    List<EffectTemplateData> Effects,
    List<SpellData> Spells
);
