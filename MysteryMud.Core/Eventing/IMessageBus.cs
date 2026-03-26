namespace MysteryMud.Core.Eventing;

public interface IMessageBus : IMessageWriter
{
    public void Process(SystemContext ctx, GameState gameState);
}
