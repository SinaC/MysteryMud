namespace MysteryMud.Core.Services;

public interface IMessageTargetBuilder
{
    void Send(string text);
    IActMessageBuilder Act(string format);
}
