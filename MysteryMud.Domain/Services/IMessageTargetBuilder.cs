namespace MysteryMud.Domain.Services;

public interface IMessageTargetBuilder
{
    void Send(string text);
    IActMessageBuilder Act(string format);
}
