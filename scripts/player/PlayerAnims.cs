using Godot;
using System;

[GlobalClass]
public partial class PlayerAnims : AnimationTree
{
    public enum HeldItemType : int
    {
        None = 0,
        Pistol = 1,
        Rifle = 2,
        SmallItem = 3,
    }

    [Export]
    public float walking = 0f;
    [Export]
    public Vector2 walkingDirection = Vector2.Zero;
    [Export]
    public HeldItemType heldItem;
    [Export]
    public float animLerpSpeed = 5f;
    [Export]
    public float rotationLerpSpeed = 1f;
    [Export]
    public LookAtModifier3D lookModify;

    [Obsolete]
    [Export]
    public bool shoot = false;

    [Obsolete]
    [Export]
    public float gunBlend = 0f;

    [ExportGroup("State Machine")]
    [Export]
    public StringName armsStatePath;
    [Export]
    public StringName legsStatePath;
    [Export]
    public StringName walkStateName;
    [Export]
    public StringName jumpStateName;
    [Export]
    public StringName pistolStateName;
    [Export]
    public StringName rifleStateName;
    [Export]
    public StringName smallItemStateName;

    [Export]
    public StringName[] walkPaths = new StringName[0];

    [ExportGroup("Blend Tree")]
    [Export]
    public StringName walkSpeedPath;
    [Export]
    [Obsolete]
    public string walk2dPath = "Walk2D/blend_position";
    [Export]
    [Obsolete]
    public string gunWalkPath;
    [Export]
    [Obsolete]
    public string gunBlendPath = "GunBlend/blend_amount";
    [Export]
    [Obsolete]
    public string gunStatePath = "Gun/playback";
    [Export]
    [Obsolete]
    public string gunShootPath = "shoot";

    public Vector3 initialPosition;
    public Quaternion initialRotation;
    public HeldItemType lastHeldType;

    private Node3D parent;
    private Node3D root;
    private Skeleton3D skeleton;
    private double updateTimer = 0d;

    public override void _EnterTree()
    {
        base._EnterTree();
        // NetworkManager.Instance.OnProcess += Tick;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        // NetworkManager.Instance.OnProcess -= Tick;
    }

    public override void _Ready()
    {
        // CallbackModeProcess = AnimationCallbackModeProcess.Manual;
        Node3D root = GetNode<Node3D>(RootNode);
        initialPosition = root.Position;
        initialRotation = root.Quaternion;
        UpdateStateMachine(heldItem);
        lastHeldType = heldItem;
        if (!RootMotionTrack.IsEmpty)
        {
            Skeleton3D skeleton = root.GetNodeOrNull<Skeleton3D>(RootMotionTrack.GetConcatenatedNames());
            if (IsInstanceValid(skeleton))
            {
                skeleton.ResetBonePoses();
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        updateTimer -= delta;
        if (updateTimer <= 0d)
        {
            // updateTimer = GD.RandRange(0.03d, 0.1d);
            updateTimer = 0.1d;
            // Advance(updateTimer);
        }
        Tick(delta);
    }

    public void Tick(double delta)
    {
        if (Active)
        {
            LerpSet(walkSpeedPath, walking, delta);
            foreach (var path in walkPaths)
            {
                LerpSet(path, walkingDirection, delta);
            }
        }

        /*
        if (IsInstanceValid(NetworkPlayer.LocalInstance) && IsInstanceValid(root))
        {
            float far = NetworkPlayer.LocalInstance.Controller.Camera.Far;
            Active = NetworkPlayer.LocalInstance.PlayerPosition.DistanceSquaredTo(root.GlobalPosition) < far * far;
            if (IsInstanceValid(skeleton))
                skeleton.Visible = Active;
        }
        */

        if (heldItem != lastHeldType)
        {
            UpdateStateMachine(heldItem);
            lastHeldType = heldItem;
        }

        if (!IsInstanceValid(parent))
            parent = GetParentOrNull<Node3D>();
        if (!IsInstanceValid(root))
            root = GetNode<Node3D>(RootNode);
        Vector3 pos = GetRootMotionPositionAccumulator();
        Quaternion rot = GetRootMotionRotationAccumulator();
        Vector3 scl = GetRootMotionScaleAccumulator();
        if (!RootMotionTrack.IsEmpty && Active)
        {
            if (!IsInstanceValid(skeleton) && IsInstanceValid(root))
            {
                skeleton = root.GetNodeOrNull<Skeleton3D>(RootMotionTrack.GetConcatenatedNames());
                if (IsInstanceValid(skeleton))
                {
                    skeleton.TopLevel = true;
                }
            }
            if (IsInstanceValid(skeleton))
            {
                int idx = skeleton.FindBone(RootMotionTrack.GetConcatenatedSubNames());
                if (IsInstanceValid(lookModify) && IsInstanceValid(parent) && idx != -1)
                {
                    // skeleton.TopLevel = true;
                    skeleton.Position = root.GlobalPosition;
                    // root.Position = parent.GlobalPosition;
                    // var scale = skeleton.Scale;
                    // skeleton.Position = pos - skeleton.GetBoneGlobalRest(idx).Origin;
                    // skeleton.GlobalRotation = (root.Quaternion * rot.Normalized()).GetEuler();
                    skeleton.SetBonePoseRotation(idx, rot);
                    // skeleton.Scale = scale;
                    float y = skeleton.Rotation.Y;
                    float y2 = root.GlobalRotation.Y;
                    float diff = Mathf.AngleDifference(y, y2);
                    float diffAbs = Mathf.Abs(diff);
                    if (!Mathf.IsZeroApprox(walking))
                    {
                        y = Mathf.LerpAngle(y, y2, (float)delta * rotationLerpSpeed);
                    }
                    else if (diff > lookModify.PrimaryLimitAngle)
                    {
                        y = Mathf.LerpAngle(y, y2, (float)delta * diffAbs * rotationLerpSpeed);
                    }
                    else if (diff < -lookModify.PrimaryLimitAngle)
                    {
                        y = Mathf.LerpAngle(y, y2, (float)delta * diffAbs * rotationLerpSpeed);
                    }
                    skeleton.Rotation = new Vector3(root.GlobalRotation.X, y, root.GlobalRotation.Z);
                    // root.Scale = parent.GlobalBasis.Scale;
                }
                // int idx = skeleton.FindBone(RootMotionTrack.GetConcatenatedSubNames());
                if (idx != -1)
                {
                    // skeleton.Position = root.GlobalPosition + pos;
                    // rot = Quaternion.FromEuler(new Vector3(0f, rot.GetEuler().Y, 0f));
                    // skeleton.SetBonePoseRotation(idx, rot);
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        /*
        if (IsInstanceValid(skeleton))
        {
            if (IsInstanceValid(lookModify))
            {
                // skeleton.TopLevel = true;
                skeleton.Position = root.GlobalPosition;
            }
        }
        */
    }

    public void ShootGun()
    {
        var gunPlayback = (AnimationNodeStateMachinePlayback)Get(gunStatePath);
        if (IsInstanceValid(gunPlayback))
        {
            gunPlayback.Start(gunShootPath);
        }
    }

    private void PlayJumpAnim(bool landing)
    {
        var playback = (AnimationNodeStateMachinePlayback)Get(legsStatePath);
        if (IsInstanceValid(playback))
        {
            if (landing)
                playback.Travel(walkStateName);
            else
                playback.Travel(jumpStateName);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    public void RpcJump(bool landing)
    {
        PlayJumpAnim(landing);
    }

    private void UpdateStateMachine(HeldItemType type)
    {
        var playback = (AnimationNodeStateMachinePlayback)Get(armsStatePath);
        if (IsInstanceValid(playback))
        {
            switch (type)
            {
                case HeldItemType.None:
                    playback.Travel(walkStateName);
                    break;
                case HeldItemType.Pistol:
                    playback.Travel(pistolStateName);
                    break;
                case HeldItemType.Rifle:
                    playback.Travel(rifleStateName);
                    break;
                case HeldItemType.SmallItem:
                    playback.Travel(smallItemStateName);
                    break;
            }
        }
    }

    public void LerpSet(StringName path, float value, double delta)
    {
        Set(path, Mathf.Lerp(Get(path).AsSingle(), value, delta * animLerpSpeed));
    }

    public void LerpSet(StringName path, Vector2 value, double delta)
    {
        Set(path, Get(path).AsVector2().Lerp(value, (float)delta * animLerpSpeed));
    }
}
