#if false
using Godot;
using System;
using System.Linq;

public partial class SCP457Controller : FPController, IPlayerSCP
{
    [Export]
    public Node3D[] disableLocal = Array.Empty<Node3D>();
    [Export(PropertyHint.Layers3DRender)]
    public uint localCullFlags = uint.MinValue;

    [Export]
    public RayCast3D playerRayCast;
    [Export]
    public GameSound killSound;
    [Export]
    public GameEffect killedEffect;
    [Export]
    public Timer attackCooldown;
    [Export]
    public float damageAmount = 10f;
    [Export]
    public float fuelDamageAmount = 20f;
    [Export]
    public float syncFireAmount = 1f;
    [Export]
    public FireWorldEffects fireWorld;
    [Export]
    public float fuelAmount = 0f;
    [Export]
    public float maxFuelAmount = 100f;

    public float ExtraEffectiveSpeed = 1f;

    protected Vector3 lastPosition;

    public override void _Ready()
    {
        base._Ready();
        if (IsMultiplayerAuthority())
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
        if (IsMultiplayerAuthority())
        {
            syncFireAmount = sprintStamina / Player.Role.MaxSprintStamina;
        }
        fireWorld.FireAmount = Mathf.Lerp(fireWorld.FireAmount, Mathf.Clamp(syncFireAmount, 0.2f, 1f), (float)delta);
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
                targetWalk = Velocity.Length() / EffectiveSpeed;
            }
            model.anims.walkingDirection = Player.Inputs.MovementDirection;
            model.anims.walking = targetWalk;
            lastPosition = Position;
        }
    }

    public void TryKillAsScp(IPlayerController victim)
    {
        if (victim.Player != null && victim != this && victim.Player.Role.team != Player.Role.team)
        {
            RpcId(1, nameof(BurnPlayer), victim.Player.AbsolutePath, sprintStamina);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void BurnPlayer(NodePath peerId, float fuel)
    {
        if (Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority() && Multiplayer.IsServer() && attackCooldown.IsStopped())
        {
            var victim = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AbsolutePath == peerId);
            // TODO: ac checks here
            if (victim.Role.team == Player.Role.team)
                return;
            attackCooldown.Start();
            // if (fuel <= 0f)
            //     return;
            RpcId(GetMultiplayerAuthority(), nameof(RpcSetFuel), Mathf.Clamp(fuel - fuelDamageAmount, 0f, Player.Role.MaxSprintStamina));
            victim.Damage(new DamageInfo(damageAmount, Player, DamageType.Scp457));
            StatusEffectBase effect = victim.statusEffectManager.EnableEffect(EffectType.Burn, 2.5f, 1f);
            if (IsInstanceValid(effect))
            {
                effect.Source = Player;
            }
            int idx = Array.IndexOf(Player.Sounds, killSound);
            if (idx != -1)
            {
                Player.Rpc(nameof(NetworkPlayer.CL_PlaySound3D), idx);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcSetFuel(float amount)
    {
        if (IsMultiplayerAuthority() && Multiplayer.GetRemoteSenderId() == 1)
        {
            sprintStamina = amount;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcAddFuel(float amount)
    {
        if (IsMultiplayerAuthority() && Multiplayer.GetRemoteSenderId() == 1)
        {
            sprintStamina = Mathf.Clamp(sprintStamina + amount, 0f, Player.Role.MaxSprintStamina);
        }
        // fuelAmount = Mathf.Clamp(fuelAmount + amount, 0f, maxFuelAmount);
    }

    public override void HandleInputs(double delta)
    {
        HandleUse(delta);
        HandleSCP457(delta);
    }

    public void HandleSCP457(double delta)
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
                    if (health is PlayerHitbox hp && hp.HP is IPlayerController plr)
                    {
                        TryKillAsScp(plr);
                    }
                    else if (health is IPlayerController plr2)
                    {
                        TryKillAsScp(plr2);
                    }
                }
            }
        }
    }

    public override float GetSpeedMultiplier()
    {
        return base.GetSpeedMultiplier() * ExtraEffectiveSpeed;
    }

    public override bool CanSprint()
    {
        return base.CanSprint();
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