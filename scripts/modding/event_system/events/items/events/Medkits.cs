public interface IMedkitEvent : IWorldItemEvent
{
    public Medkit Medkit { get; internal set; }
}


public class EventMedkitUsing : EventItemUsing, IMedkitEvent
{
    public Medkit Medkit { get; set; }

    public float HealAmount { get; set; }

    public bool IsClient { get; internal set; }
}

public class EventMedkitUsed : EventItemUsed, IMedkitEvent
{
    public Medkit Medkit { get; set; }
}