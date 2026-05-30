public interface IDoorEvent : IInteractableEvent
{
    public IDoorStatus DoorStatus { get; set; }
}

public class BaseDoorCancelableEvent : BaseCancelableEvent, IDoorEvent
{
    public IDoorStatus DoorStatus { get; set; }

    public IInteractable Interactable { get; set; }
}

public class BaseDoorEvent : BaseEvent, IDoorEvent
{
    public IDoorStatus DoorStatus { get; set; }

    public IInteractable Interactable { get; set; }
}