using MysteryMud.Domain.Commands;

namespace MysteryMud.Domain.Components.Characters;

public class CommandBuffer // this MUST be a class (see below explanation)
{
    // this cannot be used
    public CommandRequest[] Items = default!;
    public int Count;
}

// we could stick to struct when using a List instead of manual buffer (items+count)

//This line:
//buffer.Count = 0;
//not sticking means:
//Arch is iterating components in a way where your struct is not guaranteed to be written back after mutation
//This is especially likely because:
//You're mutating nested data (Items[]) AND a value field (Count)
//Arch may:
//copy the struct into a local
//pass it as ref
//but not detect it as “changed” for write-back
