using System;
using Godot;

public partial class NpcPath
{
    public Rid NavMap { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 TargetPosition { get; private set; }
    public bool IsFinished => pathIndex >= path.Length;
    public float PathMaxDistance { get; set; } = 1f;
    public Action OnWaypoint { get; set; }
    public Vector3[] path = Array.Empty<Vector3>();
    public int pathIndex = 0;

    public NpcPath()
    {
    }

    public NpcPath(Rid nav)
    {
        NavMap = nav;
    }

    public void SetTargetPosition(Vector3 target)
    {
        TargetPosition = target;
        pathIndex = 0;
        path = NavigationServer3D.MapGetPath(NavMap, Position, TargetPosition, true);
        // Array.Reverse(path);
    }

    public Vector3 NextPoint(Vector3 newPos, Vector3 velocity)
    {
        Position = newPos;
        if (IsFinished)
            return TargetPosition;
        Vector3 point = path[pathIndex];
        if (point.DistanceTo(Position) <= PathMaxDistance || (!velocity.IsZeroApprox() && (point - Position).Dot(velocity) > 0f))
        {
            OnWaypoint?.Invoke();
            pathIndex++;
        }
        if (IsFinished)
            return TargetPosition;
        return path[pathIndex];
    }
}