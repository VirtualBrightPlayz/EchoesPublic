using System.Linq;
using Godot;

[GlobalClass]
public partial class BoneProxySync : PhysicalBone3D, IGrabbable
{
    [Export]
    public BoneSync MainBone;
    [Export]
    public Node sync;

    public Vector3 lastPosition;
    public Vector3 lastRotation;
    
    public override void _EnterTree()
    {
        base._EnterTree();
        lastPosition = Position;
        if (IsInstanceValid(MainBone))
        {
            MainBone.RegisterBone(this);
        }
    }

    private bool _isGrabbed = false;
    
    public void OnGrabbed(bool state)
    {
        _isGrabbed = state;
        // sync.SyncActive = !state;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_isGrabbed)
        {
            Position = MainBone.Position + lastPosition;
            Rotation = lastRotation;
        }
        else
        {
            lastPosition = Position - MainBone.Position;
            lastRotation = Rotation;
        }
    }

    public override void _Ready()
    {
        if (!MainBone.sync.IsMultiplayerAuthority())
        {
            // AxisLockLinearX = true;
            // AxisLockLinearY = true;
            // AxisLockLinearZ = true;
            // AxisLockAngularX = true;
            // AxisLockAngularY = true;
            // AxisLockAngularZ = true;
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(sync))
        {
            // sync.SyncPosition = MainBone.sync.SyncPosition;
            // sync.SyncRotation = MainBone.sync.SyncRotation;
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (!IsInstanceValid(MainBone))
            return;
        if (/*!MainBone.sync.CanClaim &&*/ MainBone.sync.IsMultiplayerAuthority())
        {
            for (int i = 0; i < MainBone.grabbingPeers.Count; i++)
            {
                var plr = MainBone.grabbingPeers[i];
                if (!IsInstanceValid(plr as GodotObject))
                {
                    MainBone.grabbingPeers.RemoveAt(i);
                    i--;
                    continue;
                }
                float plrMass = 10f;
                float plrInertia = 45f;
                float plrAccel = 80f;
                var head = plr.View;
                Vector3 grabbedPosLocal = Vector3.Zero;
                if (MainBone.grabPoints.ContainsKey(plr.Player.GetMultiplayerAuthority()))
                {
                    grabbedPosLocal = MainBone.grabPoints[plr.Player.GetMultiplayerAuthority()];
                }
                Vector3 origin = MainBone.ToGlobal(grabbedPosLocal);
                Vector3 impl = (head.GlobalPosition - head.GlobalBasis.Z.Normalized() * 1.5f) - origin;
                state.LinearVelocity = state.LinearVelocity.MoveToward(impl * plrMass * state.InverseMass, state.Step * plrAccel);

                Basis grabRot = Basis.Identity;
                if (MainBone.grabRotations.ContainsKey(plr.Player.GetMultiplayerAuthority()))
                {
                    grabRot = MainBone.grabRotations[plr.Player.GetMultiplayerAuthority()];
                }
                Quaternion quat = (head.GlobalBasis * (MainBone.GlobalBasis * grabRot).Inverse()).GetRotationQuaternion();
                Vector3 rot = quat.GetAxis() * quat.GetAngle();
                state.AngularVelocity = state.AngularVelocity.MoveToward(rot * Mathf.DegToRad(plrInertia) * state.InverseInertia, state.Step * plrAccel);
            }
        }
    }

    public void Grab(bool grab, Vector3 localPos = default, Vector3 releaseForce = default)
    {
        MainBone.Grab(grab, MainBone.ToLocal(ToGlobal(localPos)), releaseForce);
    }

    public new double Mass => base.Mass;

    public bool AllowGrab { get; set; } = false;
    
    public Node Sync => sync;

    public bool CurrentlyGrabbed => false;
}