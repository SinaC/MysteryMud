namespace MysteryMud.Core.Eventing;

public interface IEventBuffer<TEvent>
{
    ref TEvent Add();

    Span<TEvent> GetAll();

    void Clear();
}
