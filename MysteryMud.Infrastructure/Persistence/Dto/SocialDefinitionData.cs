namespace MysteryMud.Infrastructure.Persistence.Dto;

internal record SocialDefinitionData
(
    string Name,
    string? CharacterNoArg,
    string? OthersNoArg,
    string? CharacterFound,
    string? OthersFound,
    string? VictimFound,
    string? CharacterNotFound,
    string? CharacterAuto,
    string? OthersAuto
);
