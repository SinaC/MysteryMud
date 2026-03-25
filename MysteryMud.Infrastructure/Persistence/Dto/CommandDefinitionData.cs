namespace MysteryMud.Infrastructure.Persistence.Dto;

public record CommandDefinitionData(
    string Name,
    string[] Aliases,
    string RequiredLevel,
    string MinimumPosition,
    int Priority,
    bool AllowAbbreviation
);
