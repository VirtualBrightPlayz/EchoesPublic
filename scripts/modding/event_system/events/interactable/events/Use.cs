public class EventUsing : BaseCancelableInteractableEvent
{
    public IItemHolder Holder { get; internal set; }

    public ItemObject Item { get; internal set; }
}

public class EventUsed : BaseInteractableEvent
{
    public IItemHolder Holder { get; internal set; }

    public ItemObject Item { get; internal set; }
}

