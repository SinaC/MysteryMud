using MysteryMud.Infrastructure.Persistence.Dto.Effects;
using MysteryMud.Infrastructure.Persistence.Dto.Spells;

namespace MysteryMud.Infrastructure.Persistence.Dto;

public record SpellAndEffectRootData(
    List<EffectTemplateData> Effects,
    List<SpellData> Spells
);
