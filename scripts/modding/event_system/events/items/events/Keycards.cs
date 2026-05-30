using System.Collections.Generic;

public interface IKeycardEvent : IWorldItemEvent
{
    public Keycard Keycard { get; set; }
}

public class EventKeycardCanAccess : WorldItemEvent, IKeycardEvent
{
    public Keycard Keycard { get; set; }

    public IReadOnlyCollection<KeycardAccess> KeycardAccess { get; internal set; }

    public bool Result { get; set; }
}