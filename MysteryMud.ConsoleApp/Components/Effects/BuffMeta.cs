namespace MysteryMud.ConsoleApp.Components.Effects;

struct BuffMeta
{
    public StackPolicy Policy;
}

enum StackPolicy
{
    RefreshDuration,
    Replace,
    Stack
}
