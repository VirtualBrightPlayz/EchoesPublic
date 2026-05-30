/// <summary>
/// Represents a specific event source. This allows you to control the flow of your event emitter without having to cast or check with is/as.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISpecificEventSource<in T> : IEventSource where T : IEvent
{
    public bool Emit(T evt);
}