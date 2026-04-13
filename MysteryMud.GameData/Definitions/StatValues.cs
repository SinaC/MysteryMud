using MysteryMud.GameData.Enums;
using System.Runtime.CompilerServices;

namespace MysteryMud.GameData.Definitions;

[InlineArray((int)StatKind.Count)]
public struct StatValues
{
    private int _first; // requirement of the [InlineArray] attribute

    public int this[StatKind kind]
    {
        readonly get => this[(int)kind];
        set => this[(int)kind] = value;
    }

    public static StatValues From(params (StatKind Kind, int Value)[] values)
    {
        var result = new StatValues();
        foreach (var (kind, value) in values)
            result[kind] = value;
        return result;
    }
}