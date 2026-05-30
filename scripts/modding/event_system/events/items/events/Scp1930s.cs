public interface IScp1930Event : IWorldItemEvent
{
    public Scp1930 Scp1930 { get; set; }
}

public class Event1930Deciding : WorldItemEventCancelable, IScp1930Event
{
    public Scp1930 Scp1930 { get; set; }

    public Scp1930.Scp1930Decision Decision { get; set; }

    public float Amount { get; set; }
}