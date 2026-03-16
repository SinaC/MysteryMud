using Arch.Core;

namespace MysteryMud.ConsoleApp.Systems;

class CommandBuffer
{
    private List<Action<World>> commands = new();

    public void Add(Action<World> cmd)
    {
        commands.Add(cmd);
    }

    public void Playback(World world)
    {
        foreach (var cmd in commands)
            cmd(world);

        commands.Clear();
    }
}
