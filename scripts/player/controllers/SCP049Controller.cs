#if false
using Godot;
using System;
using System.Linq;

public partial class SCP049Controller : FPController, IPlayerSCP
{
    [Export]
    public Node3D[] disableLocal = Array.Empty<Node3D>();
    [Export(PropertyHint.Layers3DRender)]
    public uint localCullFlags = uint.MinValue;

    [Export]
    public RayCast3D playerRayCast;
    [Export]
    public GameSound attackSound;
    [Export]
    public GameEffect killedEffect;
    [Export]
    public float DamageAmount = 10f;
    public float ExtraSpeed = 1f;
    [Export]
    public Timer attackCooldown;
    
    [Export]
    public float ragdollReviveDistance = 3f;

    protected Vector3 lastPosition;

    public override void _Ready()
    {
        base._Ready();
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
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (model != null && model.anims != null && Player.HasAuthority)
        {
            float targetWalk;
            if (Player.Inputs.MovementDirection.IsZeroApprox())
            {
                targetWalk = 0f;
            }
            else
            {
                targetWalk = Velocity.Length() / Speed;
            }
            model.anims.walkingDirection = Player.Inputs.MovementDirection;
            model.anims.walking = targetWalk;
            lastPosition = Position;
        }
    }

    public override float GetSpeedMultiplier()
    {
        return base.GetSpeedMultiplier() * ExtraSpeed;
    }

    public override void HandleInputs(double delta)
    {
        HandleUse(delta);
        HandleSCP049(delta);
        HandlePlayerHotkeys(delta);
    }

    public void TryKillAsScp(IPlayerController victim)
    {
        if (victim.Player != null && victim != this && victim.Player.Role.team != Player.Role.team && victim is Node node)
        {
            RpcId(1, nameof(RpcTryKill), node.GetPath());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcShoot()
    {
        if (Multiplayer.GetRemoteSenderId() == 1 && IsInstanceValid(model) && IsInstanceValid(model.anims))
        {
            model.anims.shoot = true;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcRevive(NodePath peerId)
    {
        if (!Multiplayer.IsServer())
            return;
        Node node = GetNodeOrNull(peerId);
        if (IsInstanceValid(node) && node is Ragdoll ragdoll && ragdoll.Role.team != TeamID.SCP)
        {
            var victims = IPlayerList.List(this).PlayerList.Where(x => x.RoleIndex == (int)RoleID.Spectator).ToArray();
            ragdoll.GetParent().QueueFree();
            if (victims.Length == 0)
                return;
            var victim = victims[(int)(GD.Randi() % victims.Length)];
            victim.SV_Spawn(RoleID.SCP049_2, ragdoll.skeleton.GlobalPosition);
        }
    }

    public void HandleSCP049(double delta)
    {
        playerRayCast.ForceRaycastUpdate();
        if (Player.Inputs.Primary.HasFlag(ButtonInputFlags.JustPressed) && playerRayCast.IsColliding())
        {
            var collider = playerRayCast.GetCollider();
            if (collider != this && collider is Node colliderNode)
            {
                IHealth health = IHealth.GetHealth(collider);
                if (health != null)
                {
                    RpcId(MultiplayerPeer.TargetPeerServer, nameof(RpcTryKill), colliderNode.GetPath());
                }
            }
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
        attackCooldown.Start();
        victim.Damage(new DamageInfo(victim.MaxHealth * 2f, Player, DamageType.Scp049));
    }

    private void _DamagePlayer(IPlayerController victim)
    {
        if (victim.Player.Role.team == TeamID.Dead || victim.Player.Role.team == TeamID.SCP)
            return;
        if (victim is IHealth hp)
        {
            _DamageIHealth(hp);
            int idx = Array.IndexOf(Player.Sounds, attackSound);
            if (idx != -1)
            {
                Player.Rpc(nameof(NetworkPlayer.CL_PlaySound3D), idx);
            }
        }
    }

    public override Vector3 GetMovementVelocity(Vector3 vel, double delta)
    {
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
}
#endif