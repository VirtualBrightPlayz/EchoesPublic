using System;
using System.Collections.Generic;

namespace Godot;

[GlobalClass]
public partial class ExplodingPhysicsProp3D : PhysicsProp3D
{
    [Export(PropertyHint.Range, "0,1")]
    public float ExplodePercentThreshold { get; set; } = 0.5f;
    
    [Export]
    public float ExplodeTimer { get; set; } = 5.0f;
    
    [Export]
    private float _initialExplodeTimer { get; set; } 
    
    [Export]
    public bool CountingDown { get; set; } = false;
    
    [Export]
    public Curve DamageFalloff { get; set; }
    
    [Export]
    public Area3D ExplosionShape { get; set; }
    
    [Export]
    public float DamageAmount = 100f;

    [Export]
    public float MaxDistance = 10f;

    [Export]
    public GameEffect ExplosionEffect;

    [Export]
    public float DefaultVelocity { get; private set; } = 4.0f;
    
    [Export]
    public FireWorldEffects FireEffect { get; set; }
    
    public override void _EnterTree()
    {
        base._EnterTree();
        if (!AllowDamage)
        {
            Log.PrintWarn("ExplodingPhysicsProp3D only supports taking damage.");
            return;
        }

        FireEffect.FireAmount = 0f;
        FireEffect.Enabled = false;
        _initialExplodeTimer = ExplodeTimer;
        CountingDown = false;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (CountingDown)
        {
            if (!FireEffect.Enabled)
            {
                FireEffect.Enabled = true;
            }
            FireEffect.FireAmount = 1f - ExplodeTimer / _initialExplodeTimer;
        }

        if (Mathf.IsZeroApprox(ExplodeTimer) && !IsMultiplayerAuthority())
        {
            HideProp();
        }
        
        if (!CountingDown)
        {
            return;
        }
        else
        {
            FireEffect.Enabled = true;
        }
        
        if (!IsMultiplayerAuthority())
        {
            return;
        }
        
        ExplodeTimer -= (float)delta;

        if (ExplodeTimer <= 0f)
        {
            Explode();
            return;
        }
    }
    
    public virtual void Explode()
    {
        if (Multiplayer.IsServer())
        {
            ExplodeTimer = 0f;
            CountingDown = false;
            List<IHealth> preventDups = new List<IHealth>();
            foreach (var body in ExplosionShape.GetOverlappingBodies())
            {
                // if (body is IHealth hp && body is CollisionObject3D obj)
                {

                    Vector3 end = body.GlobalPosition;
                    if (body is IPlayerController ctrl)
                    {
                        end = ctrl.View.GlobalPosition;
                    }

                    if (body is RigidBody3D rb1)
                    {
                        end = body.GlobalPosition + rb1.CenterOfMass;
                    }

                    PhysicsRayQueryParameters3D args = new PhysicsRayQueryParameters3D()
                    {
                        CollisionMask = ItemManager.Instance.playerLayer,
                        From = GlobalPosition,
                        To = end,
                        HitFromInside = false,
                        HitBackFaces = false,
                        Exclude = new Godot.Collections.Array<Rid>()
                        {
                            GetRid(),
                        }
                    };
                    var result = GetWorld3D().DirectSpaceState.IntersectRay(args);
                    if (result.Count == 0)
                    {
                        IHealth health = IHealth.GetHealth(body);
                        if (health != null)
                        {
                            if (preventDups.Contains(health))
                            {
                                continue;
                            }

                            preventDups.Add(health);
                        }

                        ExplodeNode(body, end);
                    }
                    else if ((result.TryGetValue("collider", out Variant val) && body == val.As<Node>()))
                    {
                        var collider = val.As<Node3D>();
                        IHealth health = IHealth.GetHealth(collider);
                        if (health != null)
                        {
                            if (preventDups.Contains(health))
                            {
                                continue;
                            }

                            preventDups.Add(health);
                        }

                        ExplodeNode(collider, end);
                    }
                }
            }

            var up = Vector3.Up;
            var fwd = -GlobalBasis.Z.Normalized();
            if (Mathf.Abs(fwd.Dot(up)) > 0.95f)
                fwd = GlobalBasis.Y.Normalized();
            var rot = Basis.LookingAt(fwd, up).GetEuler();

            RoundManager.Instance.Rpc(nameof(RoundManager.RpcPlayEffect),
                Array.IndexOf(RoundManager.Instance.Data.Effects, ExplosionEffect), GlobalPosition + up * 0.1f, rot);
        }
        HideProp();
    }

    private void HideProp()
    {
        QueueFree();
        Mesh.Visible = false;
        CollisionLayer = 0;
        CollisionMask = 1;
        FireEffect.Enabled = false;
    }
    
    public virtual void ExplodeNode(Node3D body, Vector3 end)
    {
        float amount = DamageFalloff.Sample(end.DistanceTo(GlobalPosition) / MaxDistance);
        DamageInfo info = new DamageInfo(amount * DamageAmount, lastDamage.SourceAbsolutePath, DamageType.Unknown, TeamID.Dead);
        ExplosiveDamageModifier modifier =
            new ExplosiveDamageModifier(GlobalPosition.DirectionTo(end) * DefaultVelocity, false);
        info.AddModifier(modifier);
        if (body is ExplodingPhysicsProp3D explodingProp)
        {
            explodingProp.ExplodeTimer = explodingProp.ExplodeTimer - Random.Shared.NextSingle() * 4;
        }
        IHealth hp = IHealth.GetHealth(body);
        if (hp != null)
        {
            if (hp.Health > 0)
            {
                hp.Damage(info);
            }
        }
        if (body is RigidBody3D rb)
        {
            // if (rb is RigidbodySync sync)
            //     sync.OnImpulse(GlobalPosition.DirectionTo(end) * DefaultVelocity, Vector3.Zero);
            // else
            if (rb is IPhysicsProp prop && !prop.Affected.HasFlag(IPhysicsProp.AffectedBy.Explosion))
            {
                return;
            }
            rb.ApplyImpulse(GlobalPosition.DirectionTo(end) * DefaultVelocity);
        }
        if (body is PhysicalBone3D bone)
        {
            bone.ApplyImpulse(GlobalPosition.DirectionTo(end) * DefaultVelocity);
        }
        if (body is GrenadeBase gren)
        {
            var grenade = gren.Spawn();
            grenade.throwerPath = lastDamage.SourceAbsolutePath;
            grenade.GlobalPosition = body.GlobalPosition;
            grenade.SetDefaultVelocity(GlobalPosition.DirectionTo(end));
            gren.Item.QueueFree();
            grenade.ExplosionTimer -= amount;
        }
    }

    public override void Damage(DamageInfo info)
    {
        base.Damage(info);
        if (!AllowDamage)
        {
            return;
        }
        if (IsDead)
        {
            return;
        }
        float hpPercent = Health / MaxHealth;
        if (hpPercent <= ExplodePercentThreshold)
        {
            CountingDown = true;
            FireEffect.Enabled = true;
        }
    }

    public override void OnCustomDeath(DamageInfo info)
    {
        base.OnCustomDeath(info);
        CountingDown = true;
        FireEffect.Enabled = true;
        ExplodeTimer = Math.Clamp(Random.Shared.NextSingle(), 0f, 0.25f);
        //Explode();
    }
}