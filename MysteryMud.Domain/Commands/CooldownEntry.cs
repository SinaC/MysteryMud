namespace MysteryMud.Domain.Commands;

public struct CooldownEntry
{
    public int CommandId;
    public long ReadyAt;
}
