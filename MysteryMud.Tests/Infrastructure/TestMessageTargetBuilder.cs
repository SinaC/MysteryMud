using MysteryMud.Domain.Services;

namespace MysteryMud.Tests.Infrastructure;

internal class TestMessageTargetBuilder : IMessageTargetBuilder
{
    public string Format { get; private set; } = default!;
    public string Text { get; private set; } = default!;

    public IActMessageBuilder Act(string format)
    {
        Format = format;
        return new TestActMessageBuilder();
    }

    public void Send(string text)
    {
        Text = text;
    }
}
