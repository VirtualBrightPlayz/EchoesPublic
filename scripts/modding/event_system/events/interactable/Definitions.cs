public interface IInteractableEvent : IEvent
{
    public IInteractable Interactable { get; set; }
}

public class BaseCancelableInteractableEvent : BaseCancelableEvent, IInteractableEvent
{
    public IInteractable Interactable { get; set; }
}

public class BaseInteractableEvent : BaseEvent, IInteractableEvent
{
    public IInteractable Interactable { get; set; }
}
