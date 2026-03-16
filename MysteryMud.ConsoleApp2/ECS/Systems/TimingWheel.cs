namespace MysteryMud.ConsoleApp2.ECS.Systems;

public class TimingWheel
{
    private readonly List<Action>[] slots;
    private int current;

    public TimingWheel(int size)
    {
        slots = new List<Action>[size];

        for (int i = 0; i < size; i++)
            slots[i] = new List<Action>();
    }

    public void Schedule(int delay, Action action)
    {
        int slot = (current + delay) % slots.Length;
        slots[slot].Add(action);
    }

    public void Tick()
    {
        var list = slots[current];

        foreach (var action in list)
            action();

        list.Clear();

        current = (current + 1) % slots.Length;
    }
}
