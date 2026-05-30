using Godot;
using Godot.Collections;

[GlobalClass]
public partial class MotionSensitivePhysicsProp3D : PhysicsProp3D
{
    [Export]
    public float MaximumDistance { get; set; } = 0.25f;
    
    [Export]
    public Array<Node> TargetDestructionNodes { get; set; } = new Array<Node>();

    private Vector3 spawnLoation = Vector3.Zero;

    private bool _triggered = false;
    
    public override void _EnterTree()
    {
        spawnLoation = GlobalPosition;
        if (IsMultiplayerAuthority())
        {
            Multiplayer.PeerConnected += id =>
            {
                RpcId(id, MethodName.RpcDisturbed, _triggered);
            };
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcDisturbed(bool state)
    {
        _triggered = state;
        if (!state)
        {
            return;
        }
        foreach (var node in TargetDestructionNodes)
        {
            if (!IsInstanceValid(node))
            {
                continue;
            }
            node.QueueFree();
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!IsMultiplayerAuthority())
        {
            return;
        }
        float distance = spawnLoation.DistanceTo(GlobalPosition);
        if (MaximumDistance < distance)
        {
            _triggered = true;
            Rpc(MethodName.RpcDisturbed, _triggered);
        }
    }
}