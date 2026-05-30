/// <summary>
/// Base implementation for the <see cref="ICancelableEvent"/> interface. This class is not needed and you may implement your own. It is recommended to be used as it provides the boierplate code needed for events to function.
/// </summary>
public class BaseCancelableEvent : BaseEvent, ICancelableEvent
{
    public bool Canceled { get; private set; } = false;

    public void Cancel()
    {
        Canceled = true;
    }
    
    public void Continue()
    {
        Canceled = false;
    }
}