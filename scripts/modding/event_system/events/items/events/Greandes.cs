using Godot;
using System.Collections.Generic;

public interface IGrenadeEvent : IEvent
{
    public ThrownGrenade ThrownGrenade { get; set; }
}

public class EventGrenadeExploding : BaseCancelableEvent, IGrenadeEvent
{
    public ThrownGrenade ThrownGrenade { get; set; }

    public List<Node3D> HitObjects { get; internal set; }
}

public class EventGrenadeExploded : BaseEvent, IGrenadeEvent
{
    public ThrownGrenade ThrownGrenade { get; set; }

    public IReadOnlyCollection<Node3D> HitObjects { get; internal set; }
}

public class EventGrenadeExplodingNode : BaseCancelableEvent, IGrenadeEvent
{
    public ThrownGrenade ThrownGrenade { get; set; }

    public float Damage { get; set; }

    public Vector3 End { get; set; }

    public Node3D Node { get; internal set; }
}

public class EventGrenadeExplodedNode : BaseEvent, IGrenadeEvent
{
    public ThrownGrenade ThrownGrenade { get; set; }

    public float Damage { get; set; }

    public Vector3 End { get; internal set; }

    public Node3D Node { get; internal set; }
}

public class EventGrenadeWillExplode : BaseEvent, IGrenadeEvent
{
    public ThrownGrenade ThrownGrenade { get; set; }

    public bool Result { get; set; }
}
