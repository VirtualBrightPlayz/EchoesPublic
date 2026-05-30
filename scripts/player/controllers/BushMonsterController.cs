#if false
using Godot;
using System;
using System.Linq;

public partial class BushMonsterController : FPController, IPlayerSCP
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
    [Export]
    public AudioStream footsteps;
    
    public float ExtraSpeed = 1f;
    [Export]
    public Timer attackCooldown;
    [Export]
    public Timer tpCooldown;
    [Export] public Curve tpTimerCurve;

    [Export] public Vector3 modelPosition;
    [Export] public Vector3 modelRotation;
    [Export] public Node3D modelRoot;
    [Export] public Node3D modelRootSeen;
    [Export] public Node3D modelRootNotSeen;
    [Export] public float maxViewDistance = 30f;
    [Export] public float maxAttackRange = 3f;
    [Export] public float tooCloseToPlayerDistance = 5f;
    [Export] public float tooCloseToPlayerTimeout = 3f;
    
    protected Vector2 lastMoveDir;
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
        {
            modelPosition = GlobalPosition;
            modelRotation = GlobalRotation;
            Player.Inputs.CanOpenInventory = false;
        }
        playerRayCast.Enabled = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!IsMultiplayerAuthority())
        {
            if (IsInstanceValid(NetworkPlayer.LocalInstance))
            {
                if (!NetworkPlayer.LocalInstance.Controller.Camera.IsPositionInFrustum(modelPosition) || NetworkPlayer.LocalInstance.Role.team != TeamID.NTF)
                {
                    modelRoot.Position = modelPosition;
                    modelRoot.Rotation = modelRotation;
                    modelRoot.Visible = true;
                }
            }
            else
            {
                modelRoot.Visible = false;
            }
        }
        else
        {
            modelPosition = GlobalPosition;
            modelRotation = GlobalRotation;
            modelRoot.Visible = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
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
            RpcId(1, MethodName.RpcTryKill, victim.Player.AbsolutePath);
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
        attackCooldown.Start(2.0d);
        var victim = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AbsolutePath == peerId);
        victim.SV_Damage(Player.AbsolutePath, DamageAmount, DamageType.Scp049_2);
        Rpc(MethodName.RpcShoot);
        // attack sounds
        int idx = Array.IndexOf(Player.Sounds, attackSound);
        if (idx != -1)
        {
            Player.Rpc(NetworkPlayer.MethodName.CL_PlaySound3D, idx);
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
        bool primary = Player.Inputs.Primary.HasFlag(ButtonInputFlags.JustPressed);
        bool secondary = Player.Inputs.Secondary.HasFlag(ButtonInputFlags.JustPressed) || Player.Inputs.Reload.HasFlag(ButtonInputFlags.JustPressed);
        if (Player.Inputs.MovementDirection.Y < lastMoveDir.Y && Player.Inputs.MovementDirection.Y < -0.5f)
        {
            secondary |= true;
        }
        lastMoveDir = Player.Inputs.MovementDirection;
        if (playerRayCast.IsColliding())
        {
            var collider = playerRayCast.GetCollider();
            var health = IHealth.GetHealth(collider);
            if (collider != this && health != null && primary)
            {
                if (playerRayCast.GetCollisionPoint().DistanceTo(playerRayCast.GlobalPosition) <= maxAttackRange)
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
            else if (collider != this)
            {
                bool seen = false;
                Vector3 pos = playerRayCast.GetCollisionPoint();
                Vector3 norm = playerRayCast.GetCollisionNormal();
                foreach (var plr in IPlayerList.List(this).PlayerList)
                {
                    if (plr.Role.team == TeamID.NTF)
                    {
                        if (plr.PlayerPosition.DistanceSquaredTo(pos) < maxViewDistance * maxViewDistance && plr.Controller.Camera.IsPositionInFrustum(pos))
                        {
                            seen = true;
                            break;
                        }
                        else if (plr.PlayerPosition.DistanceSquaredTo(GlobalPosition) < maxViewDistance * maxViewDistance && plr.Controller.Camera.IsPositionInFrustum(GlobalPosition))
                        {
                            // seen = true;
                            break;
                        }
                    }
                }
                bool dontShow = false;
                if (!tpCooldown.IsStopped() || norm.Dot(Vector3.Up) < 0.5f)
                {
                    seen = true;
                    dontShow = true;
                }
                modelRootNotSeen.Position = pos;
                modelRootNotSeen.Visible = !seen && !dontShow;
                modelRootSeen.Position = pos;
                modelRootSeen.Visible = seen && !dontShow;
                if (!seen && secondary && tpCooldown.IsStopped())
                {
                    RpcId(1, MethodName.RpcMove, pos);
                }
            }
        }
        else
        {
            modelRootNotSeen.Visible = false;
            modelRootSeen.Visible = false;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcMove(Vector3 position)
    {
        if (Multiplayer.IsServer() && tpCooldown.IsStopped())
        {
            float dist = GlobalPosition.DistanceTo(position);
            float time = tpTimerCurve.Sample(dist);
            bool foundPlayer = false;
            foreach (var plr in IPlayerList.List(this).PlayerList)
            {
                if (plr.Role.team == TeamID.NTF)
                {
                    if (plr.PlayerPosition.DistanceSquaredTo(position) < tooCloseToPlayerDistance * tooCloseToPlayerDistance)
                    {
                        foundPlayer = true;
                        time = tooCloseToPlayerTimeout;
                    }
                }
            }
            tpCooldown.Start(time);
            attackCooldown.Start(1d);
            GlobalPosition = position;
            modelPosition = position;
            modelRotation = GlobalRotation;
            Player.ForceTeleportPosition(position);
            Rpc(MethodName.RpcBushMoved, time);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcBushMoved(float time)
    {
        if (Multiplayer.GetRemoteSenderId() == 1)
        {
            tpCooldown.Start(time);
            if (IsInstanceValid(footstepsAudio) && IsInstanceValid(footsteps))
            {
                if (footstepsAudio.GetStreamPlayback() is AudioStreamPlaybackPolyphonic playback)
                {
                    playback.PlayStream(footsteps);
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
            Player.Rpc(NetworkPlayer.MethodName.CL_PlayEffectAt3D, idx, Player.PlayerPosition, Vector3.Up);
        }
        base.OnKilled(info);
    }
}
#endif
