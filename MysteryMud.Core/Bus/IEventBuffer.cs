namespace MysteryMud.Core.Bus;

public interface IEventBuffer<TEvent>
{
    ref TEvent Add();

    Span<TEvent> GetAll();

    void Clear();
}
