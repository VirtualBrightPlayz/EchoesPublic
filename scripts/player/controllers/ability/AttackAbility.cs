using System;
using System.Linq;
using Godot;

[GlobalClass]
public partial class AttackAbility : BaseAbility
{
    [Export] public RayCast3D rayCast;
    [Export] public Timer attackCooldown;
    [Export] public double attackCooldownTime = 1d;
    [Export] public float damageAmount;
    [Export] public DamageType damageType = DamageType.Generic;
    [Export] public bool friendlyFire = false;
    [Export] public GameSound attackSound;
    [Export] public EffectType damageEffects = EffectType.Invalid;
    [Export] public float damageEffectDuration = 2.5f;
    [Export] public float damageEffectIntensity = 1f;

    public override void SetupFromDefinition()
    {
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "damage_amount", ref damageAmount);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "damage_type", ref damageType);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "attack_cooldown_time", ref attackCooldownTime);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "friendly_fire_allowed", ref friendlyFire);
        if (TomlExtensions.TryGetValue(AbilityDef.dataCfg, "attack_sound_name", out string attackSoundName) && !string.IsNullOrEmpty(attackSoundName) && Player.Data.Sounds.Any(x => x.ResourceName == attackSoundName))
        {
            attackSound = Player.Data.Sounds.FirstOrDefault(x => x.ResourceName == attackSoundName);
        }
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "damage_effect_type", ref damageEffects);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "damage_effect_duration", ref damageEffectDuration);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "damage_effect_intensity", ref damageEffectIntensity);
    }

    public override void _Ready()
    {
        base._Ready();
        rayCast.Enabled = false;
        rayCast.Reparent(Player.RoleController.View, false);
        rayCast.AddException(Player.MainCollider);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (IsMultiplayerAuthority())
        {
            rayCast.ForceRaycastUpdate();
            if (Player.InputPrimary.HasFlag(ButtonInputFlags.JustPressed) && rayCast.IsColliding())
            {
                var collider = rayCast.GetCollider();
                if (collider != this && collider is Node colliderNode)
                {
                    IHealth health = IHealth.GetHealth(collider);
                    if (health != null)
                    {
                        RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcTryAttack, colliderNode.GetPath());
                    }
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcTryAttack(NodePath path)
    {
        if (!Multiplayer.IsServer())
            return;
        if (IsInstanceValid(attackCooldown) && !attackCooldown.IsStopped())
            return;
        IHealth victimNode = IHealth.GetHealth(GetNodeOrNull(path));
        if (victimNode == null)
            return;
        if (!_CanDamage(victimNode))
            return;
        if (victimNode is IPlayerController victim)
        {
            if (_CanDamagePlayer(victim))
            {
                _DamagePlayer(victim);
            }
        }
        else if (victimNode is PlayerHitbox hitbox && hitbox.HP is IPlayerController ctrl)
        {
            if (_CanDamagePlayer(ctrl))
            {
                _DamagePlayer(ctrl);
            }
        }
        else
        {
            _DamageIHealth(victimNode);
        }
    }

    protected virtual void _DamageIHealth(IHealth victim)
    {
        if (IsInstanceValid(attackCooldown) && attackCooldownTime > 0d)
        {
            attackCooldown.Start(attackCooldownTime);
        }
        victim.Damage(new DamageInfo(damageAmount, Player, damageType));
    }

    protected virtual bool _CanDamagePlayer(IPlayerController victim)
    {
        return !(victim.Player.Role.team == TeamID.Dead || (victim.Player.Role.team == Player.Role.team && !friendlyFire));
    }

    protected virtual bool _CanDamage(IHealth victim)
    {
        return true;
    }

    protected virtual void _DamagePlayer(IPlayerController victim)
    {
        if (victim is IHealth hp)
        {
            _DamageIHealth(hp);
            if (damageEffects != EffectType.Invalid)
            {
                StatusEffectBase effect = victim.Player.statusEffectManager.EnableEffect(damageEffects, damageEffectDuration, damageEffectIntensity);
                if (IsInstanceValid(effect))
                {
                    effect.Source = Player;
                }
            }
            int idx = Player.Data.Sounds.IndexOf(attackSound);
            if (idx != -1)
            {
                Player.Rpc(nameof(NetworkPlayer.CL_PlaySound3D), idx);
            }
        }
    }
}
