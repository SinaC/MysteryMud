namespace MysteryMud.Infrastructure.Persistence.Dto;

public record CommandDefinitionData(
    string Name,
    string[] Aliases,
    int RequiredLevel,
    int MinimumPosition,
    int Priority,
    bool AllowAbbreviation
);
