public class EventDoorToggling : BaseDoorCancelableEvent
{
    public bool CurrentState { get; set; }

    public bool NewState { get; set; }
}

public class EventDoorToggled : BaseDoorEvent
{
    public bool CurrentState { get; internal set; }
}

public class EventDoorTryUsing : BaseDoorCancelableEvent
{
    public Keycard Keycard { get; internal set; }
}

public class EventDoorUsed : BaseDoorEvent
{
    public Keycard Keycard { get; internal set; }
}

public class EventDoorUseFailed : BaseDoorEvent
{
    public BasePlayer Player { get; internal set; }
}

public class EventDoorUpdatingState : BaseDoorCancelableEvent
{
    public bool TargetState { get; set; }

    public bool Force { get; set; }
}

public class EventDoorUpdatedState : BaseDoorCancelableEvent
{
    public bool TargetState { get; internal set; }

    public bool Force { get; internal set; }
}

public class EventDoorLockedStateUpdating : BaseDoorCancelableEvent
{
    public bool LockedCurrent { get; internal set; }

    public bool LockedNext { get; set; }
}

public class EventDoorLockedStateUpdated : BaseDoorEvent
{
    public bool Locked { get; internal set; }
}