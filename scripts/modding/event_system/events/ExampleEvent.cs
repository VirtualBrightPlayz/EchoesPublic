internal sealed class ExampleEvent : BaseCancelableEvent
{
    public IPlayerController Controller { get; set; }
    
    public IAbility Ability { get; set; }
}

internal sealed class ExampleEmitter : IEventSource, ISpecificEventSource<ExampleEvent>
{
    public bool Emit(ExampleEvent evt)
    {
        evt.Ability = new AttackAbility();
        evt.Controller = new PlayerController();
        return EventManager.Emit(evt, this);
    }

    public bool Emit(IEvent evt)
    {
        // you may wish to cast to a specific type of event.
        return EventManager.Emit(evt, this);
    }
}