using System;
using Godot;
using Godot.Collections;

public partial class NPCTest : CharacterBody3D, IDamageSource
{
    [Export]
    public NavigationAgent3D agent;
    [Export]
    public float walkSpeed = 1f;
    [Export]
    public float accel = 10f;

    public string AttackerDisplayName => "Test NPC";
    public NodePath AbsolutePath => GetPath();

    public Node3D target => NetworkPlayer.LocalInstance.ActiveController.Root;

    private float gravity = 1f;
    private Vector3 pos;

    public override void _Ready()
    {
        gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        agent.WaypointReached += OnWaypoint;
        // RoundManager.Instance.EventOnRoundStart += () => OnWaypoint(new Dictionary());
        // await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
        // agent.TargetPosition = target.GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (agent.IsNavigationFinished())
        {
            OnWaypoint(new Dictionary());
            return;
        }
        Vector3 targetPosition = agent.GetNextPathPosition();
        Vector3 direction = GlobalPosition.DirectionTo(targetPosition);

        Vector3 vel = Velocity;
        if (!IsOnFloor())
            vel.Y -= gravity * (float)delta;
        else
            vel.Y = 0f;
        float y = vel.Y;
        direction.Y = 0f;
        direction = direction.Normalized();
        vel = vel.MoveToward(direction * walkSpeed, accel * (float)delta);
        vel.Y = y;
        Velocity = vel;

        Vector3 val = this.MoveAndStairs(delta, Velocity, Vector3.Up);
        GlobalPosition += val;
        MoveAndSlide();

        if (Velocity.DistanceTo(Vector3.Zero) < 0.1f)
            OnWaypoint(new Dictionary());
    }

    private void UpdateTarget()
    {
        pos = target.GlobalPosition;
        agent.TargetPosition = target.GlobalPosition;
    }

    private void OnWaypoint(Dictionary details)
    {
        if (IsInstanceValid(target) && pos.DistanceTo(target.GlobalPosition) > 1f /*&& !(details.TryGetValue("position", out var val) && val.AsVector3().DistanceTo(GlobalPosition) < 1f)*/)
        {
            CallDeferred(MethodName.UpdateTarget);
        }
    }
}