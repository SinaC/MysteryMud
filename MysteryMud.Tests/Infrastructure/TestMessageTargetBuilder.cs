using MysteryMud.Domain.Services;

namespace MysteryMud.Tests.Infrastructure;

internal class TestMessageTargetBuilder : IMessageTargetBuilder
{
    public IActMessageBuilder Act(string format)
        => new TestActMessageBuilder();

    public void Send(string text)
    {
    }
}
