namespace MysteryMud.Infrastructure.Persistence.Dto;

public record DotData(
    string DamageFormula,
    string DamageType,
    int TickRate // in ticks
);
