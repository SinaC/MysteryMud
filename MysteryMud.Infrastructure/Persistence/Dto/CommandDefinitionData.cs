namespace MysteryMud.Infrastructure.Persistence.Dto;

public record CommandDefinitionData(
    string Name,
    string[] Aliases,
    bool CannotBeForced,
    string RequiredLevel,
    string MinimumPosition,
    int Priority,
    bool AllowAbbreviation,
    string HelpText,
    string[] Syntaxes,
    string[] Categories,
    string[] ThrottlingCategories
);
