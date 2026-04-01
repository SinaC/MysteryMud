using MysteryMud.Domain.Commands;

namespace MysteryMud.Domain.Components.Characters;

public struct CommandBuffer
{
    public CommandRequest[] Items;
    public int Count;
}
