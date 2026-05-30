/// <summary>
/// Represents a Cancelable event. This event can also be consumed like the base <see cref="IEvent"/>.
/// </summary>
public interface ICancelableEvent : IEvent
{
    /// <summary>
    /// Determines if the event has been canceled.
    /// </summary>
    public bool Canceled { get; }

    /// <summary>
    /// Cancels the event.
    /// </summary>
    public void Cancel();

    /// <summary>
    /// Continues the event (Un-cancels it).
    /// </summary>
    public void Continue();
}