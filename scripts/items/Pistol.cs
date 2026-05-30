using System;
using Godot;

[GlobalClass]
public partial class Pistol : GunBase
{
    [Signal]
    public delegate void OnReloadAnimEventHandler();

    [Export]
    public GameSound gunshotSound;
    [Export]
    public GameEffect gunshotEffect;
    [Export]
    public GameEffect bulletHoleEffect;
    [Export]
    public GameEffect bloodSprayEffect;
    [Export]
    public GameEffect reloadEffect;
    [Export]
    public Curve damageDistanceCurve;
    [Export]
    public PlayerAnims.HeldItemType holdType = PlayerAnims.HeldItemType.Pistol;
    [Export]
    public Node3D fireEffectPoint;

    public override void Shoot(IItemHolder holder, bool applyRecoil)
    {
        base.Shoot(holder, applyRecoil);
        if (applyRecoil)
        {
            if (holder is VRPhysicsHand)
            {
                PlayRemoteShootEffects(Item.Player);
            }
            if (Item.viewModel is Viewmodel view)
            {
                view.ViewmodelShoot();
            }
            Rpc(MethodName.RpcOnFire);
        }
    }

    public void PlayRemoteShootEffects(BasePlayer player)
    {
        player.model.anims.ShootGun();
        int idx = player.Data.Sounds.IndexOf(gunshotSound);
        if (idx != -1)
        {
            gunshotSound.PlayOneShot3D(fireEffectPoint);
        }
        int idx2 = Array.IndexOf(player.Effects, gunshotEffect);
        if (idx2 != -1)
        {
            gunshotEffect.PlayOneShot3D(fireEffectPoint, true);
        }
    }

    public void PlayRemoteReloadEffects(BasePlayer player)
    {
        EmitSignalOnReloadAnim();
        if (Item.ViewModelEnabledCached && Item.viewModel is Viewmodel viewmodel)
        {
            viewmodel.ViewmodelReload();
        }
        if (!player.IsLocalPlayer)
        {
            int idx2 = Array.IndexOf(player.Effects, reloadEffect);
            if (idx2 != -1)
            {
                reloadEffect.PlayOneShot3D(fireEffectPoint, true);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void RpcOnFire()
    {
        if (Item.SenderIsPlayer)
            PlayRemoteShootEffects(Item.Player);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcOnReload()
    {
        if (Item.SenderIsServer)
            PlayRemoteReloadEffects(Item.Player);
    }

    public override void ReloadStarted()
    {
        base.ReloadStarted();
        Rpc(MethodName.RpcOnReload);
    }

    public override void OnExit(SimulatedProjectile projectile, PhysicsIntersectionUtility3D.HitResult hitResult)
    {
        var attacker = Item.Player;
        var obj = hitResult.Collider;
        int idx = Array.IndexOf(attacker.Effects, bulletHoleEffect);
        if (idx != -1)
        {
            if (attacker.HasAuthority)
                attacker.RpcId(1, nameof(NetworkPlayer.SV_PlayEffectAt3D), idx, hitResult.HitLocation, hitResult.HitNormal);
            else
                attacker.Rpc(nameof(NetworkPlayer.CL_PlayEffectAt3D), idx, hitResult.HitLocation, hitResult.HitNormal);
        }
        base.OnExit(projectile, hitResult);
    }

    public override void OnHit(SimulatedProjectile projectile, PhysicsIntersectionUtility3D.HitResult hitResult)
    {
        var attacker = Item.Player;
        var obj = hitResult.Collider;
        int idx = Array.IndexOf(attacker.Effects, bulletHoleEffect);
        if (idx != -1)
        {
            if (attacker.HasAuthority)
                attacker.RpcId(1, nameof(NetworkPlayer.SV_PlayEffectAt3D), idx, hitResult.HitLocation, hitResult.HitNormal);
            else
                attacker.Rpc(nameof(NetworkPlayer.CL_PlayEffectAt3D), idx, hitResult.HitLocation, hitResult.HitNormal);
        }
        int bidx = Array.IndexOf(attacker.Effects, bloodSprayEffect);
        if (bidx != -1 && (obj is IPlayerController || obj is PlayerHitbox))
        {
            if (attacker.HasAuthority)
                attacker.RpcId(1, nameof(NetworkPlayer.SV_PlayEffectAt3D), bidx, hitResult.HitLocation, hitResult.HitNormal);
            else
                attacker.Rpc(nameof(NetworkPlayer.CL_PlayEffectAt3D), bidx, hitResult.HitLocation, hitResult.HitNormal);
        }
        base.OnHit(projectile, hitResult);
    }

    public override void Hit(BasePlayer attacker, GodotObject obj, Vector3 hitPosition, Vector3 hitNormal)
    {
        int idx = Array.IndexOf(attacker.Effects, bulletHoleEffect);
        if (idx != -1)
        {
            if (attacker.HasAuthority)
                attacker.RpcId(1, nameof(NetworkPlayer.SV_PlayEffectAt3D), idx, hitPosition, hitNormal);
            else
                attacker.Rpc(nameof(NetworkPlayer.CL_PlayEffectAt3D), idx, hitPosition, hitNormal);
        }
        int bidx = Array.IndexOf(attacker.Effects, bloodSprayEffect);
        if (bidx != -1 && (obj is IPlayerController || obj is PlayerHitbox))
        {
            if (attacker.HasAuthority)
                attacker.RpcId(1, nameof(NetworkPlayer.SV_PlayEffectAt3D), bidx, hitPosition, hitNormal);
            else
                attacker.Rpc(nameof(NetworkPlayer.CL_PlayEffectAt3D), bidx, hitPosition, hitNormal);
        }
        base.Hit(attacker, obj, hitPosition, hitNormal);
    }

    public override float CalcHitDamage(BasePlayer attacker, GodotObject obj, Vector3 hitPosition, Vector3 hitNormal)
    {
        if (IsInstanceValid(damageDistanceCurve))
        {
            float dist = attacker.PlayerPosition.DistanceTo(hitPosition);
            float dmg = damageDistanceCurve.Sample(dist / MaxRange);
            return GunDamage * dmg;
        }
        return base.CalcHitDamage(attacker, obj, hitPosition, hitNormal);
    }
}