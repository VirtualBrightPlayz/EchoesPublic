#if false
using Godot;
using System;
using System.Linq;

public partial class SCP049_2Controller : FPController, IPlayerSCP
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
    [Export]
    public float PropThrowStrength = 15f;
    
    public float ExtraSpeed = 1f;
    [Export]
    public Timer attackCooldown;

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
        HandleSCP049_2(delta);
    }

    public void TryKillAsScp(IPlayerController victim)
    {
        if (victim.Player != null && victim != this && victim.Player.Role.team != Player.Role.team)
        {
            RpcId(1, nameof(RpcTryKill), victim.Player.AbsolutePath);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcTryKill(NodePath peerId)
    {
        if (!Multiplayer.IsServer())
            return;
        if (Multiplayer.GetRemoteSenderId() != GetMultiplayerAuthority())
            return;
        if (!attackCooldown.IsStopped())
            return;
        attackCooldown.Start();
        var victim = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AbsolutePath == peerId);
        victim.SV_Damage(Player.AbsolutePath, DamageAmount, DamageType.Scp049_2);
        Rpc(nameof(RpcShoot));
        // attack sounds
        int idx = Array.IndexOf(Player.Sounds, attackSound);
        if (idx != -1)
        {
            Player.Rpc(nameof(NetworkPlayer.CL_PlaySound3D), idx);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcShoot()
    {
        if (Multiplayer.GetRemoteSenderId() == 1 && IsInstanceValid(model) && IsInstanceValid(model.anims))
        {
            model.anims.ShootGun();
            model.anims.shoot = true;
        }
    }

    public void HandleSCP049_2(double delta)
    {
        playerRayCast.ForceRaycastUpdate();
        if (Player.Inputs.Primary.HasFlag(ButtonInputFlags.JustPressed) && playerRayCast.IsColliding())
        {
            var collider = playerRayCast.GetCollider();
            var health = IHealth.GetHealth(collider);
            if (collider != this)
            {
                if (health is IPlayerController victim)
                {
                    TryKillAsScp(victim);
                }
                else if (health is PlayerHitbox hitbox && hitbox.HP is IPlayerController ctrl)
                {
                    TryKillAsScp(ctrl);
                }
                else if (health is IPhysicsProp prop)
                {
                    Vector3 vec =
                        View.GlobalPosition.DirectionTo(((Node3D)(collider)).GlobalPosition) * PropThrowStrength;
                    prop.OnImpulse(vec, default);
                }
                else
                {
                    health.Damage(new DamageInfo(DamageAmount, Player, DamageType.Scp049_2));
                }
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