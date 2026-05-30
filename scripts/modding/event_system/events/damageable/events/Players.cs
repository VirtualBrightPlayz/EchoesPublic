public interface IPlayerDamageableEvent : IDamageableEvent
{
    public BasePlayer Player { get; set; }
}

public class EventPlayerDying : EventDamageableDying, IPlayerDamageableEvent
{
    public BasePlayer Player { get; set; }
}

public class EventPlayerDeath : EventDamageableDeath, IPlayerDamageableEvent
{
    public BasePlayer Player { get; set; }
}

public class EventPlayerHealing : EventDamageableHealing, IPlayerDamageableEvent
{
    public BasePlayer Player { get; set; }
}

public class EventPlayerHealed : EventDamageableHealed, IPlayerDamageableEvent
{
    public BasePlayer Player { get; set; }
}

public class EventPlayerDamaging : EventDamageableDamaging, IPlayerDamageableEvent
{
    public BasePlayer Player { get; set; }
}

public class EventPlayerDamaged : EventDamageableDamaged, IPlayerDamageableEvent
{
    public BasePlayer Player { get; set; }
}