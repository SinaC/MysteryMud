namespace MysteryMud.Infrastructure.Persistence.Dto;

public readonly struct SocialDefinitionData
{
    public required string Name { get; init; }
    public required string? CharacterNoArg { get; init; }
    public required string? OthersNoArg { get; init; }
    public required string? CharacterFound { get; init; }
    public required string? OthersFound { get; init; }
    public required string? VictimFound { get; init; }
    public required string? CharacterNotFound { get; init; }
    public required string? CharacterAuto { get; init; }
    public required string? OthersAuto { get; init; }
}
