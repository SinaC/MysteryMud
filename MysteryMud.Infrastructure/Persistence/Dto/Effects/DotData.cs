namespace MysteryMud.Infrastructure.Persistence.Dto.Effects;

public record DotData(
    string DamageFormula,
    string DamageType,
    int TickRate // in ticks
);
