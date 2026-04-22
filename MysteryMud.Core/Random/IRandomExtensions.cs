namespace MysteryMud.Core.Random;

public static class IRandomExtensions
{
    // NextPercent() → 0..99, so Chance(100) always true, Chance(0) always false
    public static bool Chance(this IRandom rng, int chancePercent)
        => rng.NextPercent() < chancePercent;

    public static int Range(this IRandom rng, int min, int max)
        => rng.Next(min, max + 1);

    public static int Fuzzy(this IRandom rng, int number)
    {
        int result = rng.Next(0, 4) switch
        {
            0 => number - 1,
            3 => number + 1,
            _ => number
        };
        return Math.Max(1, result);
    }

    public static int Dice(this IRandom rng, int count, int sides)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sides);

        int total = 0;
        for (int i = 0; i < count; i++)
            total += rng.Next(1, sides + 1);
        return total;
    }

    public static int Dice(this IRandom rng, int count, int sides, int modifier)
        => rng.Dice(count, sides) + modifier;
}
