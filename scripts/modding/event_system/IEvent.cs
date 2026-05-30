/// <summary>
/// Represents the minimum requirements for an event to exist. These events cannot be canceled, only <see cref="Consumed"/>. See <see cref="ICancelableEvent"/> for a decisive event.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// The source of the event.
    /// </summary>
    public IEventSource Source { get; set; }
    
    /// <summary>
    /// If the event has been consumed. If true, the event will not propagate down the list of events.
    /// </summary>
    public bool Consumed { get; }
    
    /// <summary>
    /// Consumes the event. This prevents the event from propagating down the list of events.
    /// </summary>
    public void Consume();
}