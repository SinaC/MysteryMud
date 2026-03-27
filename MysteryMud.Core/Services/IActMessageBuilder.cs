namespace MysteryMud.Core.Services;

public interface IActMessageBuilder
{
    void With(params object[] args);
}
