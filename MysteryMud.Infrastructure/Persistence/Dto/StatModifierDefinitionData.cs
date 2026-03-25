namespace MysteryMud.Infrastructure.Persistence.Dto;

public record StatModifierDefinitionData(

    string Stat,
    string Type,
    int Value // TODO: formula ?
);
