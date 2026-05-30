using Godot;

public interface IGunEvent : IWorldItemEvent
{
    public GunBase GunBase { get; internal set; }
}

public class EventShooting : WorldItemEventCancelable, IGunEvent
{
    public bool IsClient { get; internal set; }

    public GunBase GunBase { get; set; }

    public IItemHolder ItemHolder { get; internal set; }

    public bool ApplyRecoil { get; set; }

    public float FireRate { get; set; }

    public Rect2 Recoil { get; set; }

    public Vector2 Spread { get; set; }
}

public class EventShot : WorldItemEvent, IGunEvent
{
    public bool IsClient { get; internal set; }

    public GunBase GunBase { get;  set; }

    public IItemHolder ItemHolder { get; internal set; }

    public bool ApplyRecoil { get; internal set; }

    public float FireRate { get; internal set; }

    public Rect2 Recoil { get; internal set; }

    public Vector2 Spread { get; internal set; }
}

public class EventShotHitting : WorldItemEventCancelable, IGunEvent
{
    public SimulatedProjectile SimulatedProjectile { get; internal set; }

    public PhysicsIntersectionUtility3D.HitResult HitResult { get; internal set; }

    public Node3D Node { get; internal set; }

    public IHealth HealthObject { get; internal set; }

    public GunBase GunBase { get; set; }
}

public class EventShotHit : WorldItemEvent, IGunEvent
{
    public SimulatedProjectile SimulatedProjectile { get; internal set; }

    public PhysicsIntersectionUtility3D.HitResult HitResult { get; internal set; }

    public Node3D Node { get; internal set; }

    public IHealth HealthObject { get; internal set; }

    public IPhysicsProp PhysicsProp { get; internal set; }

    public GunBase GunBase { get; set; }
}