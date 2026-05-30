public interface IDamageableEvent : IEvent
{
    public IHealth Victim { get; set; }

    public IDamageSource Damager { get; set; }

    public IHealSource Healer { get; set; }

    public IHealthModifier Modifier { get; set; }
}

public class DamageableEventBase : BaseEvent, IDamageableEvent
{
    public IHealth Victim { get; set; }

    public IDamageSource Damager { get; set; }

    public IHealSource Healer { get; set; }

    public IHealthModifier Modifier { get; set; }
}

public class DamageableCancelableEventBase : BaseCancelableEvent, IDamageableEvent
{
    public IHealth Victim { get; set; }

    public IDamageSource Damager { get; set; }

    public IHealSource Healer { get; set; }

    public IHealthModifier Modifier { get; set; }
}

public class EventDamageableDying : DamageableCancelableEventBase
{

}

public class EventDamageableDeath : DamageableEventBase
{

}

public class EventDamageableHealing : DamageableCancelableEventBase
{

}

public class EventDamageableHealed : DamageableEventBase
{

}

public class EventDamageableDamaging : DamageableCancelableEventBase
{

}

public class EventDamageableDamaged : DamageableEventBase
{

}