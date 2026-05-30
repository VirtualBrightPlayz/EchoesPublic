/// <summary>
/// Represents the minimum implementation needed for an <see cref="IEvent"/>. This class is not needed to use the event system, but it is recommended to be used with it.
/// </summary>
public class BaseEvent : IEvent
{
    public IEventSource Source { get; set; }
    
    public bool Consumed { get; private set; } = false;

    public void Consume()
    {
        Consumed = true;
    }
}