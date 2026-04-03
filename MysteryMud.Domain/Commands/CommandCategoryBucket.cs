namespace MysteryMud.Domain.Commands;

public struct CommandCategoryBucket
{
    public float Tokens;
    public float MaxTokens;
    public float RefillRate;

    public long LastRefillTime;
}
