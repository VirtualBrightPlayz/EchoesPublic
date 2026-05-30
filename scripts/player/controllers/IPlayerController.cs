using Godot;

public interface IPlayerController
{
    public Node3D Root => this as Node3D;
    bool IsControllerActive => GodotObject.IsInstanceValid(Player) && Player.ActiveController == this;
    Node3D View { get; }
    Node3D Floor { get; }
    Camera3D Camera { get; }
    BasePlayer Player { get; set; }

    public static bool IsSeenBy(IPlayerController target, Node3D body)
    {
        if (GodotObject.IsInstanceValid((GodotObject)target) && GodotObject.IsInstanceValid(body) && body != target && body is IPlayerController player && body is PhysicsBody3D pbody)
        {
            Vector3 origin = player.Camera.GlobalPosition;
            Vector3 end = target.Camera.GlobalPosition;
            float f = player.Camera.Far;
            if (end.DistanceSquaredTo(origin) > f * f)
            {
                return false;
            }
            if (!player.Camera.IsPositionInFrustum(end) && !player.Camera.IsPositionInFrustum(target.Player.PlayerPosition))
            {
                return false;
            }
            PhysicsRayQueryParameters3D ray = new PhysicsRayQueryParameters3D()
            {
                From = origin,
                To = end,
                Exclude = new Godot.Collections.Array<Rid>(new Rid[] { pbody.GetRid() }),
                CollisionMask = ItemManager.Instance.playerLayer,
            };
            var result = body.GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (result.Count > 0)
            {
                var collider = result["collider"].AsGodotObject();
                if (collider.Equals(target))
                {
                    return true;
                }
            }
            else
                return true;
        }
        return false;
    }
}

public interface IPlayerControllerExt : IPlayerController
{
    void OnDamaged(DamageInfo info);
    void OnKilled(DamageInfo info);
}
