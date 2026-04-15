namespace MysteryMud.Domain.Services;

public interface IActMessageBuilder
{
    void With(params object[] args);
}
