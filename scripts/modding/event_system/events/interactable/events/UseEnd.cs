public class EventUseEnding : BaseCancelableInteractableEvent
{
    public IItemHolder Holder { get; internal set; }

    public ItemObject Item { get; internal set; }
}

public class EventUseEnded : BaseInteractableEvent
{
    public IItemHolder Holder { get; internal set; }

    public ItemObject Item { get; internal set; }
}