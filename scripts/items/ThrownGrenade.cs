using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class ThrownGrenade : RigidbodySync, IElevatorTeleport, ISpecificEventSource<IGrenadeEvent>, IDamageSource
{
	public const float UpdateInterval = 0.1f;

	// This is for SCP-018
	[Export]
	public bool SpeedUp { get; private set; } = false;

	[Export]
	public float SpeedUpMaxVelocity { get; set; } = 15f;
	[Export]
	public Curve SpeedUpOverTime;
	[Export]
	public double SpeedUpMaxTime = 0d;

	[Export]
	public GpuParticles3D trails;

	[Export]
	public bool Sticky { get; private set; } = false;

	[Export]
	public bool ExplodeOnContact { get; set; } = false;

	[Export]
	public float SpeedUpSpeed { get; set; } = 1.5f;

	[Export]
	public float DamageVelocity { get; private set; } = 0.0f;

	[Export]
	public float DefaultVelocity { get; private set; } = 4.0f;

	[Export]
	public float ExplosionTimer;

	[Export]
	public Area3D ExplosionShape;

	[Export]
	public Curve DamageFalloff;
	[Export]
	public float DamageAmount = 100f;

	[Export]
	public float MaxDistance = 10f;

	[Export]
	public GameEffect ExplosionEffect;

	[Export]
	public GameSound HitEffect;
	[Export]
	public NodePath throwerPath;
	[Export]
	public TeamID throwerTeam = TeamID.Dead;

	private bool _doSync = true;
	private bool didCollide = false;
	private Vector3 normal = Vector3.Up;
	private Node3D hitNode;
	private Vector3 hitPoint = Vector3.Zero;
	public double timeAlive = 0d;

	public bool DoSync
	{
		get => _doSync;
		set
		{
			SetPhysics(value);
			_doSync = value;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		Freeze = !IsMultiplayerAuthority();
	}

	public override void _EnterTree()
	{
		BodyEntered += OnCollide;
	}

	public override void _ExitTree()
	{
		BodyEntered -= OnCollide;
	}

	private void OnCollide(Node body)
	{
		if (IsMultiplayerAuthority() && DoSync)
		{
			Rpc(nameof(RpcCollide), GlobalPosition);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void RpcCollide(Vector3 pos)
	{
		if (Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority() && IsInstanceValid(HitEffect))
		{
			HitEffect.PlayOneShotAt3D(ItemManager.Instance, pos);
		}
	}

	private void SetPhysics(bool value)
	{
		foreach (var ownerId in GetShapeOwners())
		{
			if (ShapeOwnerGetOwner((uint)ownerId) is CollisionShape3D shape3D)
			{
				shape3D.Disabled = !value;
			}
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		base._IntegrateForces(state);
		if (IsMultiplayerAuthority() && state.GetContactCount() != 0 && ExplodeOnContact && timeAlive > 0.25d)
		{
			ExplosionTimer = 0f;
			return;
		}
		if (IsMultiplayerAuthority() && timeAlive > 1d && DamageVelocity > 0f)
		{
			for (int i = 0; i < state.GetContactCount(); i++)
			{
				var obj = state.GetContactColliderObject(i);
				IHealth hp = IHealth.GetHealth(obj);
				if (hp != null)
				{
					// Log.Print("hit", DamageVelocity * state.LinearVelocity.Length());
					hp.Damage(new DamageInfo(DamageVelocity * state.LinearVelocity.Length(), throwerPath, DamageType.Crushed, TeamID.Dead));
				}
			}
		}
		var SpeedUpVelocity = state.LinearVelocity * SpeedUpSpeed;
		if (SpeedUp && state.GetContactCount() != 0)
		{
			// prob not the best way to do this but whatever
			state.LinearVelocity = SpeedUpVelocity.LimitLength(SpeedUpOverTime.Sample((float)(timeAlive / SpeedUpMaxTime)) * SpeedUpMaxVelocity);
		}
		if (Sticky && state.GetContactCount() != 0 && !didCollide && xformSync.IsClaimantServer())
		{
			Node node = GetNodeOrNull(throwerPath);
			Node hit = state.GetContactColliderObject(0) as Node;
			if (IsInstanceValid(node) && node.IsAncestorOf(hit) && timeAlive < 0.5d)
			{
			}
			else
			{
				normal = state.GetContactLocalNormal(0);
				hitNode = state.GetContactColliderObject(0) as Node3D;
				if (IsInstanceValid(hitNode))
				{
					hitPoint = hitNode.ToLocal(state.GetContactColliderPosition(0));
				}
				GravityScale = 0f;
				didCollide = true;
			}
		}
		if (Sticky && !xformSync.IsClaimantServer())
		{
			didCollide = false;
			GravityScale = 1f;
			hitNode = null;
		}
		if (Sticky && didCollide && xformSync.IsClaimantServer())
		{
			if (IsInstanceValid(hitNode))
			{
				var xform = state.Transform;
				xform.Origin = hitNode.ToGlobal(hitPoint);
				// state.LinearVelocity = (hitNode.ToGlobal(hitPoint) - state.Transform.Origin) / state.Step;
				state.Transform = xform;
			}
			state.LinearVelocity = Vector3.Zero;
			state.AngularVelocity = Vector3.Zero;
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (!DoSync)
			return;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsInstanceValid(trails))
		{
			Vector3 vel = xformSync.GetVelocity();
			if (vel.IsZeroApprox())
			{
				vel = LinearVelocity;
			}
			trails.EmitParticle(GlobalTransform, vel.Normalized() * 0.3f, Colors.White, new Color(0f, 0f, 0f, 0.3f), (uint)(GpuParticles3D.EmitFlags.Position | GpuParticles3D.EmitFlags.Velocity));
		}

		if (!IsMultiplayerAuthority())
		{
			return;
		}

		timeAlive += delta;

		if (!Sticky || didCollide)
		{
			ExplosionTimer -= (float)delta;
		}
		if (ExplosionTimer <= 0f)
		{
            ExplosionTimer = 10f;
            EventGrenadeWillExplode evt = EventManager.GetInstance<EventGrenadeWillExplode>();
			evt.Result = true;
			Emit(evt);
            if (!evt.Result)
			{
                return;
			}
			Explode();
			return;
		}

		return;

		var oldVelocity = LinearVelocity;
		var SpeedUpVelocity = oldVelocity * SpeedUpSpeed;
		var collisionInfo = MoveAndCollide(LinearVelocity * (float)delta);
		
		if (SpeedUp)
		{
			// prob not the best way to do this but whatever
			LinearVelocity = new Vector3(LinearVelocity.X < SpeedUpVelocity.X ? SpeedUpVelocity.X : LinearVelocity.X,
				LinearVelocity.Y < SpeedUpVelocity.Y ? SpeedUpVelocity.Y : LinearVelocity.Y,
				LinearVelocity.Z < SpeedUpVelocity.Z ? SpeedUpVelocity.Z : LinearVelocity.Z);
		}

		if (collisionInfo != null)
		{
			LinearVelocity = LinearVelocity.Bounce(collisionInfo.GetNormal());
		}
	}

	public virtual void Explode()
	{
		if (!Multiplayer.IsServer())
		{
			return;
		}
		List<IHealth> preventDups = new List<IHealth>();
		List<Node3D> targets = new List<Node3D>();
		foreach (var body in ExplosionShape.GetOverlappingBodies())
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
					if (health is PlayerHitbox hitbox)
					{
						if (preventDups.Contains(hitbox.HP))
						{
							continue;
						}
						preventDups.Add(hitbox.HP);
					}
					if (preventDups.Contains(health))
					{
						continue;
					}
					preventDups.Add(health);
				}
				targets.Add(body);
				//ExplodeNode(body, end);
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
				targets.Add(body);
				//ExplodeNode(collider, end);
			}
		}

		EventGrenadeExploding exploding = EventManager.GetInstance<EventGrenadeExploding>();
		exploding.HitObjects = targets;
		if (!Emit(exploding))
		{
			return;
		}

		foreach (var body in targets)
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
			ExplodeNode(body, end);
        }

		var up = normal;
		var fwd = -GlobalBasis.Z.Normalized();
		if (Mathf.Abs(fwd.Dot(up)) > 0.95f)
			fwd = GlobalBasis.Y.Normalized();
		var rot = Basis.LookingAt(fwd, up).GetEuler();


		QueueFree();
		RoundManager.Instance.Rpc(nameof(RoundManager.RpcPlayEffect), Array.IndexOf(RoundManager.Instance.Data.Effects, ExplosionEffect), GlobalPosition + up * 0.1f, rot);

		EventGrenadeExploded evtP = EventManager.GetInstance<EventGrenadeExploded>();
		evtP.HitObjects = targets.AsReadOnly();
		Emit(evtP);
	}

	public virtual void ExplodeNode(Node3D body, Vector3 end)
	{
		EventGrenadeExplodingNode evt = EventManager.GetInstance<EventGrenadeExplodingNode>();
		evt.Node = body;
		evt.End = end;
		float amount = DamageFalloff.Sample(Mathf.Clamp(end.DistanceTo(GlobalPosition) / MaxDistance, 0f, 1f));
		evt.Damage = amount;

		if (!Emit(evt))
		{
			return;
		}

        DamageInfo info = new DamageInfo(evt.Damage * DamageAmount, throwerPath, DamageType.Crushed, SpeedUp ? TeamID.Dead : throwerTeam);
        ExplosiveDamageModifier modifier = new ExplosiveDamageModifier(GlobalPosition.DirectionTo(evt.End) * DefaultVelocity, false);
        info.AddModifier(modifier);

        IHealth hp = IHealth.GetHealth(body);
		if (hp != null)
		{
			hp.Damage(info);
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
			rb.ApplyImpulse(GlobalPosition.DirectionTo(evt.End) * DefaultVelocity);
		}
		if (body is PhysicalBone3D bone)
		{
			bone.ApplyImpulse(GlobalPosition.DirectionTo(evt.End) * DefaultVelocity);
		}
		if (body is GrenadeBase gren)
		{
			var grenade = gren.Spawn();
			grenade.throwerPath = throwerPath;
			grenade.throwerTeam = throwerTeam;
			grenade.GlobalPosition = body.GlobalPosition;
			grenade.SetDefaultVelocity(GlobalPosition.DirectionTo(evt.End));
			gren.Item.QueueFree();
			grenade.ExplosionTimer -= amount;
		}

		EventGrenadeExplodedNode evtP = EventManager.GetInstance<EventGrenadeExplodedNode>();
		evtP.End = evt.End;
		evtP.Damage = evt.Damage;
		evtP.Node = evt.Node;
		Emit(evtP);
    }

	public virtual void SetDefaultVelocity(Vector3 fwd)
	{
		LinearVelocity = fwd * DefaultVelocity;
	}

	public void ElevatorTeleport(Vector3 position, Vector3 rotation)
	{
		if (DoSync)
		{
			GlobalPosition = position;
			GlobalRotation = rotation;
		}
	}

	public virtual byte[] ToBytes()
	{
		byte[] arr = new byte[sizeof(float)];
		BinaryPrimitives.WriteSingleLittleEndian(new Span<byte>(arr, 0, sizeof(float)), ExplosionTimer);
		return arr;
	}

	public virtual void FromBytes(byte[] arr)
	{
		ExplosionTimer = BinaryPrimitives.ReadSingleLittleEndian(new Span<byte>(arr, 0, sizeof(double)));
	}

    public bool Emit(IGrenadeEvent evt)
    {
		evt.ThrownGrenade = this;
		return Emit(evt as IEvent);
    }

    public bool Emit(IEvent evt)
    {
		return EventManager.Emit(evt, this);
    }
}
