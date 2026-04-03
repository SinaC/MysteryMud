using MysteryMud.Domain.Commands;
using MysteryMud.Domain.Components.Characters.Players;

namespace MysteryMud.Domain.Factories;

public static class CommandThrottlingFactory
{
    //Movement
    //  high refill
    //  low/no lag
    //  max: 10
    //  refill: 10/sec
    //  lag: 0–100ms
    //Combat
    //  low refill
    //  noticeable lag
    //  max: 5
    //  refill: 2/sec
    //  lag: 300–800ms
    //Social
    //  basically free
    //  max: 20
    //  refill: 20/sec
    //  lag: 0
    //Utility
    //  medium refill
    //  noticeable lag
    //  max: 10
    //  refill: 5/sec
    //  lag: 500ms
    //Admin
    //  unrestricted
    public static void Initialize(ref CommandThrottle t)
    {
        t.Movement = CreateBucket(10f, 10f); // fast, responsive
        t.Combat = CreateBucket(5f, 2f);   // slower
        t.Social = CreateBucket(20f, 20f); // basically unlimited
        t.Utility = CreateBucket(10f, 5f);
        t.Admin = CreateBucket(100f, 100f); // never blocked

        t.NextAllowedTime = 0;
    }

    private static CommandCategoryBucket CreateBucket(float max, float refill)
    {
        return new CommandCategoryBucket
        {
            Tokens = max,
            MaxTokens = max,
            RefillRate = refill,
            LastRefillTime = 0
        };
    }
}