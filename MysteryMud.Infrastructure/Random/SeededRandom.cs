using MysteryMud.Core.Random;

namespace MysteryMud.Infrastructure.Random;

public sealed class SeededRandom : IRandom
{
    private readonly System.Random _rng;

    public SeededRandom(int seed) => _rng = new System.Random(seed);
    public SeededRandom() => _rng = new System.Random();          // random seed at runtime

    public int Next(int min, int max) => _rng.Next(min, max);
    public double NextDouble() => _rng.NextDouble();
    public int NextPercent() => _rng.Next(0, 100);
}
