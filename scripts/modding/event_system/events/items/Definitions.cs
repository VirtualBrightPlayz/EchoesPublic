public interface IWorldItemEvent : IEvent
{
    public WorldItem WorldItem { get; set; }
}

public class WorldItemEvent : BaseEvent, IWorldItemEvent
{
    public WorldItem WorldItem { get; set; }
}

public class WorldItemEventCancelable : BaseCancelableEvent, IWorldItemEvent
{
    public WorldItem WorldItem { get; set; }
}

public class EventItemUsing : WorldItemEventCancelable
{

}

public class EventItemUsed : WorldItemEvent
{
    
}