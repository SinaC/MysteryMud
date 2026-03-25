namespace MysteryMud.Infrastructure.Persistence.Dto.Effects;

public record StatModifierDefinitionData(

    string Stat,
    string Type,
    int Value // TODO: formula ?
);
