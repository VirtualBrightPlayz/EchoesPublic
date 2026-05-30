#if false
using Godot;
using System;
using System.Linq;

public partial class SCP173Controller : FPController, IPlayerSCP
{
    [Export]
    public Node3D[] disableLocal = Array.Empty<Node3D>();
    [Export(PropertyHint.Layers3DRender)]
    public uint localCullFlags = uint.MinValue;

    [Export]
    public RayCast3D playerRayCast;
    [Export]
    public GameSound neckSnapSound;
    [Export]
    public Area3D area;
    public bool seen;
    [Export]
    public GameEffect killedEffect;
    [Export]
    public CollisionShape3D collider;
    [Export]
    public Shape3D ventShape;
    [Export]
    public Shape3D normalShape;
    [Export]
    public Vector3 ventPosition;
    [Export]
    public Vector3 normalPosition;
    [Export]
    public Vector3 camPos;
    public float ExtraSpeed = 1f;

    [Export]
    public bool InVents
    {
        get => collider.Shape == ventShape;
        set => collider.Shape = value ? ventShape : normalShape;
    }

    [Export]
    public Node3D worldModel;
    [Export]
    public float audioLerpSpeed = 1f;

    [ExportGroup("Sync")]
    [Export]
    public Vector3 modelPosition;
    [Export]
    public Vector3 modelRotation;

    private Shape3D oldShape;

    public override void _Ready()
    {
        base._Ready();
        InVents = false;
        if (Player.IsLocalPlayer)
        {
            foreach (var item in disableLocal)
            {
                foreach (var inst in item.FindChildren("*", nameof(VisualInstance3D)))
                {
                    if (inst is VisualInstance3D vis)
                    {
                        vis.Layers = localCullFlags;
                    }
                }
            }
        }
        camera.CullMask = ~localCullFlags;
        if (IsMultiplayerAuthority())
            Player.Inputs.CanOpenInventory = false;
        playerRayCast.Enabled = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Player.HasAuthority)
        {
            // var scl = worldModel.Scale;
            // worldModel.LerpNodePosition(modelPosition, Player.lerpSpeed * (float)delta);
            // worldModel.LerpNodeRotation(modelRotation, Player.lerpSpeed * (float)delta);
            // worldModel.Scale = scl;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        collider.Position = InVents ? ventPosition : normalPosition;
        head.Position = InVents ? camPos / 2f : camPos;
        seen = IsSeen();

        if (InVents && Multiplayer.IsServer() && RoundManager.Instance.ventTimer > 0)
        {
            Damage(new DamageInfo(Player.MaxHealth * RoundManager.Instance.ventHealthPercentPerSecond * (float)delta));
        }
        ProcessFootsteps(delta);

        base._PhysicsProcess(delta);
        
        if (Player.HasAuthority || true)
        {
            if (RoundManager.Instance.IsBlinking || !seen)
            {
                var scl = worldModel.Scale;
                worldModel.GlobalPosition = GlobalPosition;
                worldModel.GlobalRotation = GlobalRotation;
                modelPosition = worldModel.GlobalPosition;
                modelRotation = worldModel.GlobalRotation;
                worldModel.Scale = scl;
            }
            worldModel.GlobalPosition = GlobalPosition;
            modelPosition = worldModel.GlobalPosition;
        }
        else
        {
            var scl = worldModel.Scale;
            worldModel.GlobalPosition = GlobalPosition;
            if (RoundManager.Instance.IsBlinking || !seen)
            {
                worldModel.GlobalRotation = GlobalRotation;
            }
            worldModel.Scale = scl;
        }
    }

    public override float GetSpeedMultiplier()
    {
        return base.GetSpeedMultiplier() * ExtraSpeed;
    }

    public override void HandleInputs(double delta)
    {
        HandleUse(delta);
        HandleSCP173(delta);
    }

    public void TryKillAsScp(IPlayerController victim)
    {
        if (victim.Player != null && victim != this && victim.Player.Role.team != Player.Role.team && victim is Node node)
        {
            RpcId(MultiplayerPeer.TargetPeerServer, nameof(RpcTryKill), node.GetPath());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcTryKill(NodePath path)
    {
        if (!Multiplayer.IsServer())
            return;
        IHealth victimNode = IHealth.GetHealth(GetNodeOrNull(path));
        if (victimNode == null)
            return;
        if (victimNode is IPlayerController victim)
        {
            _DamagePlayer(victim);
        }
        else if (victimNode is PlayerHitbox hitbox && hitbox.HP is IPlayerController ctrl)
        {
            _DamagePlayer(ctrl);
        }
        else
        {
            _DamageIHealth(victimNode);
        }
    }

    private void _DamageIHealth(IHealth victim)
    {
        victim.Damage(new DamageInfo(victim.MaxHealth * 2f, Player, DamageType.Scp173));
    }

    private void _DamagePlayer(IPlayerController victim)
    {
        if (victim.Player.Role.team == TeamID.Dead || victim.Player.Role.team == TeamID.SCP)
            return;
        if (victim is IHealth hp)
        {
            _DamageIHealth(hp);
            int idx = Array.IndexOf(Player.Sounds, neckSnapSound);
            if (idx != -1)
            {
                Player.Rpc(nameof(NetworkPlayer.CL_PlaySound3D), idx);
            }
        }
    }

    public void HandleSCP173(double delta)
    {
        if (RoundManager.Instance.IsBlinking || !seen)
        {
            playerRayCast.ForceRaycastUpdate();
            if (Player.Inputs.Primary.HasFlag(ButtonInputFlags.Pressed) && playerRayCast.IsColliding())
            {
                var collider = playerRayCast.GetCollider();
                if (collider != this && collider is Node colliderNode)
                {
                    IHealth health = IHealth.GetHealth(collider);
                    if (health != null && health is Node node)
                    {
                        RpcId(MultiplayerPeer.TargetPeerServer, nameof(RpcTryKill), node.GetPath());
                    }
                }
            }
        }
    }

    public bool IsSeenBy(Node3D body)
    {
        if (body is IPlayerController player && player.Player.Role.team == Player.Role.team)
        {
            return false;
        }

        return IPlayerController.IsSeenBy(this, body);
    }

    public bool IsSeen()
    {
        bool found = false;
        var bodies = area.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            found = IsSeenBy(body);
            if (found)
                break;
        }
        return found;
    }

    public override Vector3 GetMovementVelocity(Vector3 vel, double delta)
    {
        if (!RoundManager.Instance.IsBlinking && seen)
        {
            Vector3 velocity = vel;
            velocity.X = Mathf.MoveToward(Velocity.X, 0, EffectiveSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, EffectiveSpeed);
            return velocity;
        }
        return base.GetMovementVelocity(vel, delta);
    }

    public override void OnKilled(DamageInfo info)
    {
        int idx = Array.IndexOf(Player.Effects, killedEffect);
        if (idx != -1)
        {
            Player.Rpc(nameof(NetworkPlayer.CL_PlayEffectAt3D), idx, Player.PlayerPosition, Vector3.Up);
        }
        base.OnKilled(info);
    }

    public override void FootStep(double delta)
    {
    }

    public void ProcessFootsteps(double delta)
    {
        if (Player.HasAuthority)
        {
            if (IsOnFloor() && (!Player.Inputs.IsCrouching || Player.Role.team == TeamID.SCP))
            {
                footstepTimer += delta * Velocity.Length() / EffectiveSpeed;
                if (footstepTimer >= footstepInterval)
                {
                    footstepTimer = 0f;
                    Rpc(MethodName.RpcScrape);
                }
            }
            lastFootstepTimer = footstepTimer;
        }
        footstepsAudio.VolumeLinear = Mathf.Lerp(footstepsAudio.VolumeLinear, 0f, (float)delta * audioLerpSpeed);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void RpcScrape()
    {
        footstepsAudio.VolumeLinear = Player.HasAuthority ? 0.75f : 1f;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcSetInVents(bool vent)
    {
        if (Multiplayer.GetRemoteSenderId() == 1)
            InVents = vent;
    }

    public void SwapColliderShapes()
    {
        InVents = !InVents;
        Rpc(nameof(RpcSetInVents), InVents);
    }
}
#endif