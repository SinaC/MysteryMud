using MysteryMud.GameData.Enums;
using System.Runtime.CompilerServices;

namespace MysteryMud.Domain.Components.Rooms;

[InlineArray((int)DirectionKind.Count)]
public struct RoomExitValues
{
    private Exit? _first;

    public Exit? this[DirectionKind kind]
    {
        readonly get => this[(int)kind];
        set => this[(int)kind] = value;
    }

    public readonly bool HasAnyExit()
    {
        foreach (var exit in this)
            if (exit is not null)
                return true;
        return false;
    }
}