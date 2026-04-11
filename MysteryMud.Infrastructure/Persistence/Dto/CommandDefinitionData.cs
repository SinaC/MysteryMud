namespace MysteryMud.Infrastructure.Persistence.Dto;

internal record CommandDefinitionData(
    string Name,
    string[] Aliases,
    bool CannotBeForced,
    string RequiredLevel,
    string MinimumPosition,
    int Priority,
    bool DisallowAbbreviation,
    string HelpText,
    string[] Syntaxes,
    string[] Categories,
    string[] ThrottlingCategories
);
