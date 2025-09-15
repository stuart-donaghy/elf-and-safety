using System.Collections.Concurrent;

namespace ElfAndSafety.Persistence.Cqrs;

public interface IEventBus
{
    void Publish<TEvent>(TEvent @event);
    void Subscribe<TEvent>(Action<TEvent> handler);
}

public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Publish<TEvent>(TEvent @event)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            foreach (var handler in list.ToArray())
            {
                try
                {
                    ((Action<TEvent>)handler).Invoke(@event);
                }
                catch
                {
                    // swallow handler exceptions to avoid breaking publisher
                }
            }
        }
    }

    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (list)
        {
            list.Add(handler);
        }
    }
}

public enum UserEventType { Created, Updated, Deleted, Restored, PermanentlyDeleted }

public class UserChangedEvent
{
    public UserEventType Type { get; set; }
    public ElfAndSafety.Models.User? User { get; set; }
    public int? UserId { get; set; }
}
