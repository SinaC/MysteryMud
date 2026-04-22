using MysteryMud.Core.Random;

namespace MysteryMud.Tests.Infrastructure;

public sealed class FixedRandom : IRandom
{
    private readonly Queue<double> _values;
    public FixedRandom(params double[] values) => _values = new Queue<double>(values);

    public int Next(int min, int max) => min + (int)((_values.Dequeue()) * (max - min));
    public double NextDouble() => _values.Dequeue();
    public int NextPercent() => (int)(_values.Dequeue() * 100);
}
