public class EventCanUse : BaseInteractableEvent
{
    public IItemHolder Holder { get; internal set; }

    public ItemObject Item { get; internal set; }

    public bool Result { get; set; }
}
