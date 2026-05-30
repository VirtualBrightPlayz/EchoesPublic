using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BreakableGlass : StaticBody3D, IHealth
{
	[Export]
	public Array<GpuParticles3D> Particles;

	[Export]
	public MeshInstance3D Mesh;
	
	[Export]
	public AudioStreamPlayer3D Audio;

	[Export]
	public CollisionShape3D Shape;
	
	[Export] 
	public float Health { get; set; } = 50;

	[Export] 
	public float MaxHealth { get; set; } = 50;
	
	private bool isDead = false;

	private uint collisionLayer;

    private uint collisionMask;
    
    public override void _EnterTree()
    {
        base._EnterTree();
        collisionLayer = CollisionLayer;
        collisionMask = CollisionMask;
        SetMeta(IHealth.MetaName, this);
    }

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Health <= 0f && !isDead)
		{
			Kill(new DamageInfo());
			isDead = true;
		}
	}

	public void Spawn(HealInfo info)
	{
		Health = MaxHealth;
	}

	public void Damage(DamageInfo info)
	{
		Health -= info.Amount;
		// DebugDrawManager.Instance.DrawDebugString(GlobalPosition, info.Amount.ToString(), Colors.Red);
		foreach (var mod in info.Modifiers)
		{
			mod.Apply(this);
		}

		if (Health <= 0f)
		{
			// Kill(info);
		}
	}

	public void Heal(HealInfo info)
	{
		Health = Math.Min(MaxHealth, Health + info.Amount);
	}
	
	public void Kill(DamageInfo info)
	{
		foreach (var particle in Particles)
		{
			if (info.Source is Node3D node && particle.ProcessMaterial is ParticleProcessMaterial material)
			{
				Vector3 direction = particle.ToLocal(node.GlobalPosition.DirectionTo(particle.GlobalPosition));
				direction.Y = 1.0f;
				direction *= 3f;
				material.Direction = direction;
			}
			particle.Emitting = true;
		}
		Audio.Play();
		CollisionMask = 0;
		CollisionLayer = 0;
		Mesh.Visible = false;
		foreach (var mod in info.Modifiers)
		{
			mod.ApplyPostMortem(this);
		}
	}

	public void Hide()
	{
		foreach (var particle in Particles)
		{
			particle.Emitting = false;
		}
		Particles[0].Finished -= Hide;
	}
	
	public void Revive()
	{
		foreach (var particle in Particles)
		{
			particle.Emitting = false;
		}
		CollisionMask = collisionMask;
		CollisionLayer = collisionLayer;
		Mesh.Visible = true;
		Audio.Stop();
	}
}
