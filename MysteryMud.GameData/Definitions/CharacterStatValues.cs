using MysteryMud.GameData.Enums;
using System.Runtime.CompilerServices;

namespace MysteryMud.GameData.Definitions;

[InlineArray((int)CharacterStatKind.Count)]
public struct CharacterStatValues
{
    private int _first; // requirement of the [InlineArray] attribute

    public int this[CharacterStatKind kind]
    {
        readonly get => this[(int)kind];
        set => this[(int)kind] = value;
    }

    public static CharacterStatValues From(params (CharacterStatKind Kind, int Value)[] values)
    {
        var result = new CharacterStatValues();
        foreach (var (kind, value) in values)
            result[kind] = value;
        return result;
    }
}