namespace MysteryMud.Core.Eventing;

public interface IEventDispatcher<T>
    where T: struct
{
    void Subscribe(Action<T> handler);

    void Dispatch(ReadOnlySpan<T> events);
}
