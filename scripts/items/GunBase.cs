using System;
using System.Buffers.Binary;
using System.Linq;
using Godot;

[GlobalClass]
public partial class GunBase : WorldItem, ISpecificEventSource<IGunEvent>
{
    [Export]
    public Vector2 Spread = Vector2.One;
    [Export]
    public Vector2 SpreadAiming = Vector2.Zero;
    [Export]
    public Rect2 Recoil = new Rect2(-Vector2.One, Vector2.One);
    [Export]
    public Rect2 TimedRecoil = new Rect2(-Vector2.One, Vector2.One);
    [Export]
    public int MaxAmmo;
    [Export]
    public float ReloadTime = 2f;
    [Export]
    public float FireRate = 0.1f;
    [Export]
    public float GunDamage = 10f;
    [Export]
    public float MaxRange = 100f;
    [Export]
    public bool HoldFire = true;
    [Export]
    public DamageType TypeOfDamage = DamageType.Unknown;
    [Export]
    public AmmoType TypeOfAmmo = AmmoType.AmmoPistol;
    [Export]
    public float aimFov = 50f;
    [Export]
    public bool useAimFov = true;
    [Export]
    public int PelletsPerShot = 1;
    [Export]
    public int AmmoPerReload = 0;
    [Export]
    public float PenetrationBudget = 100f;
    [Export]
    public float BulletSpeed = 220f;
    [ExportGroup("Debug View")]
    [Export]
    public int Meta;
    [Export]
    public float Timer;
    [Export]
    public bool IsReloading = false;
    [Export]
    public bool CanFire = true;
    public bool DidFire = false;

    public int SV_AllowedShots = 0;

    public ButtonInputFlags ReloadState = ButtonInputFlags.None;

    public override string ToString()
    {
        return $"{base.ToString()}\nLoaded Ammo: {Meta}";
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Meta = MaxAmmo;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Item.PrimaryHolder != null && !Item.PrimaryHolder.IsSocket && Item.Player.IsLocalPlayer)
        {
            if (!IsReloading && CanFire)
            {
                InputManager.UpdateInput(Item.Player, LocalPlayerInput.PlayerReload, ref ReloadState);
                if (ReloadState.HasFlag(ButtonInputFlags.JustPressed))
                {
                    RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcReload);
                    return;
                }
                if (Timer > 0f)
                {
                    Timer -= (float)delta;
                }
                if (!Item.PrimaryHolder.InputPrimary.HasFlag(ButtonInputFlags.Pressed))
                {
                    DidFire = false;
                }
                else if (Item.PrimaryHolder.InputPrimary.HasFlag(ButtonInputFlags.Pressed) && !DidFire && Timer <= 0f && Meta > 0)
                {
                    // RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcShoot);
                    Shoot(Item.PrimaryHolder, true);
                    // for (int i = 1; i < PelletsPerShot; i++)
                    // {
                        // Shoot(Item.PrimaryHolder, false);
                    // }
                    DidFire = !HoldFire;
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcReload()
    {
        if (IsMultiplayerAuthority() && Item.SenderIsPlayer)
            Reload();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcShoot()
    {
        if (IsMultiplayerAuthority() && Item.SenderIsPlayer && Meta > 0)
        {
            // Meta--;
            // SV_AllowedShots += PelletsPerShot;
        }
    }

    public virtual async void Reload()
    {
        if (IsReloading || !IsInsideTree() || Meta >= MaxAmmo || !Item.Player.TryGetAbility(out InventoryAbility inventory) || !IsInstanceValid(inventory.GetAmmoItem(TypeOfAmmo)))
            return;
        IsReloading = true;
        ReloadStarted();
        await ToSignal(GetTree().CreateTimer(ReloadTime), SceneTreeTimer.SignalName.Timeout);
        IsReloading = false;
        ReloadFinished();
    }

    public virtual void ReloadStarted()
    {
    }

    public virtual void ReloadFinished()
    {
        if (!IsInstanceValid(Item.Player) || !Item.Player.TryGetAbility(out InventoryAbility inventory))
            return;
        int min = Mathf.Abs(MaxAmmo - Meta);
        if (AmmoPerReload > 0)
        {
            min = Mathf.Min(min, AmmoPerReload);
        }
        AmmoItem ammoItem = inventory.GetAmmoItem(TypeOfAmmo);
        ammoItem.amount -= min;
        if (ammoItem.amount <= 0)
        {
            ammoItem.Item.QueueFree();
        }
        Meta += min;
    }

    public virtual void Shoot(IItemHolder holder, bool applyRecoil)
    {
        float fireRate = FireRate;
        Rect2 recoil = Recoil;
        Vector2 spread = Spread;
        holder.GetPlayer().Role.ApplyGunSkills(ref fireRate, ref recoil, ref spread);
        Timer = fireRate;
        if (holder.IsAimingDown || Item.Holders.Length > 1)
            spread = SpreadAiming;

        EventShooting evt = EventManager.GetInstance<EventShooting>();
        evt.ItemHolder = holder;
        evt.FireRate = fireRate;
        evt.Spread = spread;
        evt.ApplyRecoil = applyRecoil;
        evt.IsClient = true;
        evt.GunBase = this;
        Emit(evt);
        
        if (evt.Canceled)
        {
            return;
        }

        // TODO: move this to server, but we need rollback netcode!
        Vector3 rayOrigin = holder.AimTransform.Origin;
        Vector3 rayDirection = -holder.AimTransform.Basis.Z;
        for (int i = 0; i < PelletsPerShot; i++)
        {
            Basis rotation = Basis.LookingAt(rayDirection.Normalized());
            Vector3 rngDir = new Vector3((float)GD.RandRange(-evt.Spread.X, evt.Spread.X), (float)GD.RandRange(-evt.Spread.Y, evt.Spread.Y), 0f);
            Vector3 fwd = rotation * (Vector3.Forward + rngDir);
            fwd = fwd.Normalized();
            // SimulatedProjectile projectile = new(rayOrigin, fwd * BulletSpeed, PenetrationBudget, GunDamage);
            var exclude = new Godot.Collections.Array<Rid>();
            exclude.Add(Item.PrimaryHolder.MainCollider.GetRid());
            foreach (var item in Item.Player.model.FindChildren("*", nameof(CollisionObject3D)))
            {
                if (item is CollisionObject3D col)
                    exclude.Add(col.GetRid());
            }
            PhysicsRayQueryParameters3D args = PhysicsRayQueryParameters3D.Create(rayOrigin, rayOrigin + fwd * MaxRange, ItemManager.Instance.attackLayer, exclude);
            var results = PhysicsIntersectionUtility3D.IntersectRay(ProjectileSimulationManager.Instance.Cast, args);
            if (results.IsHit)
            {
                RpcId(MultiplayerPeer.TargetPeerServer, MethodName.SV_Gun_Damage_Old, GetPathTo(results.Collider), results.HitLocation, results.HitNormal);
            }
            // projectile.RayQueryParameters.Exclude = exclude;
            // projectile.OnHit += OnHit;
            // projectile.OnPenetrationExited += OnExit;
            // ProjectileSimulationManager.Instance.AddProjectile(projectile);
        }
        // END TODO

        RpcId(MultiplayerPeer.TargetPeerServer, MethodName.SV_Shoot, holder.AimTransform.Origin, -holder.AimTransform.Basis.Z);

        if (evt.ApplyRecoil)
        {
            var player = holder.GetPlayer();
            if (holder is NetworkPlayer && player.ActiveController is PlayerController plr)
            {
                var rot = player.PlayerRotation + new Vector3(Mathf.DegToRad((float)GD.RandRange(evt.Recoil.Position.Y, evt.Recoil.Size.Y)), Mathf.DegToRad((float)GD.RandRange(evt.Recoil.Position.X, evt.Recoil.Size.X)), 0f) / plr.aimMulti;
                player.ApplyRotation(rot);
                plr.recoilTimer.Start(fireRate);
                plr.timedRecoil += new Vector2((float)GD.RandRange(TimedRecoil.Position.X, TimedRecoil.Size.X), (float)GD.RandRange(TimedRecoil.Position.Y, TimedRecoil.Size.Y));
            }
            if (holder is VRPhysicsHand hand && Item.Holders.Length <= 1)
            {
                hand.Recoil(new Vector3(Mathf.DegToRad((float)GD.RandRange(evt.Recoil.Position.Y, evt.Recoil.Size.Y)), Mathf.DegToRad((float)GD.RandRange(evt.Recoil.Position.X, evt.Recoil.Size.X)), 0f));
            }
        }

        EventShot evtShot = EventManager.GetInstance<EventShot>();
        evtShot.ItemHolder = holder;
        evtShot.FireRate = evt.FireRate;
        evtShot.Spread = evt.Spread;
        evtShot.ApplyRecoil = evt.ApplyRecoil;
        evtShot.IsClient = true;
        Emit(evtShot);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SV_Shoot(Vector3 rayOrigin, Vector3 rayDirection)
    {
        if (IsMultiplayerAuthority() && Item.SenderIsPlayer && Meta > 0)
        {
            Meta--;
            return;
            float fireRate = FireRate;
            Rect2 recoil = Recoil;
            Vector2 spread = Spread;
            Item.Player.Role.ApplyGunSkills(ref fireRate, ref recoil, ref spread);
            Timer = fireRate;
            if (Item.PrimaryHolder.IsAimingDown || Item.Holders.Length > 1)
                spread = SpreadAiming;

            EventShooting evt = EventManager.GetInstance<EventShooting>();
            evt.ItemHolder = Item.Player;
            evt.FireRate = fireRate;
            evt.Spread = spread;
            evt.ApplyRecoil = true;
            evt.IsClient = false;
            Emit(evt);

            if (evt.Canceled)
            {
                return;
            }

            for (int i = 0; i < PelletsPerShot; i++)
            {
                Basis rotation = Basis.LookingAt(rayDirection.Normalized());
                Vector3 rngDir = new Vector3((float)GD.RandRange(-evt.Spread.X, evt.Spread.X), (float)GD.RandRange(-evt.Spread.Y, evt.Spread.Y), 0f);
                Vector3 fwd = rotation * (Vector3.Forward + rngDir);
                fwd = fwd.Normalized();
                SimulatedProjectile projectile = new(rayOrigin, fwd * BulletSpeed, PenetrationBudget, GunDamage);
                var exclude = new Godot.Collections.Array<Rid>();
                exclude.Add(Item.PrimaryHolder.MainCollider.GetRid());
                foreach (var item in Item.Player.model.FindChildren("*", nameof(CollisionObject3D)))
                {
                    if (item is CollisionObject3D col)
                        exclude.Add(col.GetRid());
                }
                projectile.RayQueryParameters.Exclude = exclude;
                projectile.OnHit += OnHit;
                projectile.OnPenetrationExited += OnExit;
                ProjectileSimulationManager.Instance.AddProjectile(projectile);
            }

            EventShot shotEvt = EventManager.GetInstance<EventShot>();
            shotEvt.ItemHolder = Item.Player;
            shotEvt.FireRate = evt.FireRate;
            shotEvt.Spread = evt.Spread;
            shotEvt.ApplyRecoil = true;
            shotEvt.IsClient = false;
            Emit(shotEvt);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SV_Gun_Damage_Old(NodePath hitObj, Vector3 hitPosition, Vector3 hitNormal)
    {
        if (IsMultiplayerAuthority())
        {
            if (Item.SenderIsPlayer && Meta > 0 /*&& SV_AllowedShots > 0*/)
            {
                // SV_AllowedShots--;
                Node obj = GetNodeOrNull(hitObj);
                Hit(Item.Player, obj, hitPosition, hitNormal);
            }
        }
    }

    public virtual void OnExit(SimulatedProjectile projectile, PhysicsIntersectionUtility3D.HitResult hitResult)
    {
    }

    public virtual void OnHit(SimulatedProjectile projectile, PhysicsIntersectionUtility3D.HitResult hitResult)
    {
        var obj = hitResult.Collider;
        float damage = (float)projectile.Damage;
        IHealth health = IHealth.GetHealth(obj);

        EventShotHitting evt = EventManager.GetInstance<EventShotHitting>();
        evt.HitResult = hitResult;
        evt.SimulatedProjectile = projectile;
        evt.HealthObject = health;
        evt.Node = obj;
        Emit(evt);
        if (evt.Canceled)
        {
            return;
        } 

        if (health != null)
        {
            health.Damage(new DamageInfo(damage, Item.Player, TypeOfDamage, Item.Player.Role.team));
        }

        EventShotHit hit = EventManager.GetInstance<EventShotHit>();
        hit.HealthObject = health;
        hit.Node = obj;
        hit.SimulatedProjectile = projectile;
        hit.HitResult = hitResult;

        if (obj is IPhysicsProp prop && prop.Affected.HasFlag(IPhysicsProp.AffectedBy.Bullet))
        {
            hit.PhysicsProp = prop;
            if (obj is RigidBody3D rb)
            {
                var pushDir = -hitResult.HitNormal;
                // var velDotDiff = Velocity.Dot(pushDir) - rb.LinearVelocity.Dot(pushDir);
                // velDotDiff = Mathf.Max(0f, velDotDiff);
                float force = Mathf.Min(1f, damage / rb.Mass);
                if (obj is RigidbodySync propSync)
                {
                    propSync.OnImpulse(pushDir * force, hitResult.HitLocation - rb.GlobalPosition);
                }
            }
        }

        Emit(hit);
    }

    public virtual void Hit(BasePlayer attacker, GodotObject obj, Vector3 hitPosition, Vector3 hitNormal)
    {
        float damage = CalcHitDamage(attacker, obj, hitPosition, hitNormal);
        IHealth health = IHealth.GetHealth(obj);
        if (health != null)
        {
            health.Damage(new DamageInfo(damage, attacker, TypeOfDamage, attacker.Role.team));
        }

        if (obj is IPhysicsProp prop && prop.Affected.HasFlag(IPhysicsProp.AffectedBy.Bullet))
        {
            if (obj is RigidBody3D rb)
            {
                var pushDir = -hitNormal;
                // var velDotDiff = Velocity.Dot(pushDir) - rb.LinearVelocity.Dot(pushDir);
                // velDotDiff = Mathf.Max(0f, velDotDiff);
                float force = Mathf.Min(1f, damage / rb.Mass);
                if (obj is RigidbodySync propSync)
                {
                    propSync.OnImpulse(pushDir * force, hitPosition - rb.GlobalPosition);
                }
            }
        }
    }

    public virtual float CalcHitDamage(BasePlayer attacker, GodotObject obj, Vector3 hitPosition, Vector3 hitNormal)
    {
        return GunDamage;
    }

    public bool Emit(IGunEvent evt)
    {
        evt.GunBase = this;
        evt.WorldItem = this;
        return Emit(evt as IWorldItemEvent);
    }
}
