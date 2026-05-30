/// <summary>
/// Represents a generic event source.
/// </summary>
public interface IEventSource
{
    public bool Emit(IEvent evt);    
}