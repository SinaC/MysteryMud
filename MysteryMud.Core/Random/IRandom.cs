namespace MysteryMud.Core.Random;

public interface IRandom
{
    int Next(int minValue, int maxValue);
    double NextDouble(); // 0.0 to 1.0
    int NextPercent();   // 0 to 99, sugar method
}
