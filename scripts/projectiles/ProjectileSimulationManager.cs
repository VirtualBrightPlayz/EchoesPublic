using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class ProjectileSimulationManager : SingletonNode3D<ProjectileSimulationManager>
{
	public const double MAX_PROJECTILE_LIFETIME_SECONDS = 3f;
	public static StringName PROJECTILE_RESISTANE_MEDIUM_KEY = "projectile_resistance_medium";
	[Export]
	public int NUM_SUBSTEPS = 4;

	[Export, Description("Gravity acceleration used in simulation. Unit: m/s^2")]
	public double GravityAcceleration = 9.81f;

	[Export, Description("Medium used as air")]
	public ProjectileResistanceMedium AirMedium;

	[Export, Description("Medium used as fallback when no medium could be detected. Projectile is intended to be stopped instantly.")]
	public ProjectileResistanceMedium FallbackMedium;

	// debug options
	[Export]
	public bool ShouldDebugDraw = false;

	[Export]
	public double DebugDrawPrimitiveDuration = 10f;

	[Export]
	public double DebugDrawStringDuration = 10f;
	//

	private List<SimulatedProjectile> _simulatedProjectiles = new();
	private RayCast3D _rayCast;

	public RayCast3D Cast => _rayCast;

	public void AddProjectile(SimulatedProjectile data)
	{
		_simulatedProjectiles.Add(data);
	}

	public int NumProjectilesActive()
	{
		return _simulatedProjectiles.Count;
	}

	public void Reset()
	{
		_simulatedProjectiles.Clear();
	}

	public override void _EnterTree()
	{
		base._EnterTree();
	}

    public override void _Ready()
    {
        base._Ready();
		if (IsInstanceValid(_rayCast))
			return;
		_rayCast = new RayCast3D();
		_rayCast.Enabled = false;
		AddChild(_rayCast, true);
    }

	public override void _PhysicsProcess(double deltaTimeSeconds)
	{
		base._PhysicsProcess(deltaTimeSeconds);
		// InternalDebugInput();

		for (int i = 0; i < NUM_SUBSTEPS; i++)
			InternalStepSimulation(deltaTimeSeconds / NUM_SUBSTEPS);
	}

	private void InternalDebugInput()
	{
		// TODO: remove all this
		

		if (Input.IsKeyPressed(Key.F))
		{
			SimulatedProjectile projectile = new(GetViewport().GetCamera3D().GlobalPosition, -GetViewport().GetCamera3D().GlobalTransform.Basis.Z * 220f, 100f, 20f);
			AddProjectile(projectile);

			System.Diagnostics.Debug.WriteLine($"Now processing {_simulatedProjectiles.Count} projectiles");
		}

		if (Input.IsKeyPressed(Key.O))
		{
			RandomNumberGenerator rng = new RandomNumberGenerator();
			for (int n = 0; n < 300; n++)
			{
				float theta = Mathf.Acos(2f * rng.Randf() - 1f);
				float phi = 2f * Mathf.Pi * rng.Randf();

				Vector3 direction = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi) ).Normalized();

				SimulatedProjectile projectile = new(GetViewport().GetCamera3D().GlobalPosition, direction * 80f, 40f, 5f);

				projectile.GravityMultiplier = 5f;
				AddProjectile(projectile);
			}

			System.Diagnostics.Debug.WriteLine($"Now processing {_simulatedProjectiles.Count} projectiles");
		}
	}

	private void InternalStepSimulation(double deltaTimeSeconds)
	{
		for (int i = _simulatedProjectiles.Count - 1; i >= 0; i--)
		{
			SimulatedProjectile projectile = _simulatedProjectiles[i];
			projectile.StepSimulation(deltaTimeSeconds);

			if (projectile.ShouldBeDestroyed())
			{
				_simulatedProjectiles.RemoveAt(i);
				continue;
			}

			// Entering hit
			{
				projectile.RayQueryParameters.From = projectile.LastWorldLocation;
				projectile.RayQueryParameters.To = projectile.GetCurrentWorldLocation();

				var hitResult = PhysicsIntersectionUtility3D.IntersectRay(_rayCast, projectile.RayQueryParameters);
				if (hitResult.IsHit)
				{
					InternalDrawDebugLine(hitResult.RayStart, hitResult.HitLocation, projectile.Medium.DebugColor);
					InternalDrawDebugPoint(hitResult.HitLocation, Colors.Red);

					ProjectileResistanceMedium newMedium = hitResult.Collider.GetMeta(PROJECTILE_RESISTANE_MEDIUM_KEY, FallbackMedium).AsGodotObject() as ProjectileResistanceMedium;

					string debugString = $"Hitting: {hitResult.Collider.Name}\nEntering: {newMedium.DebugName}\nBudget: {projectile.PenetrationBudget}\nDamage: {projectile.Damage}";
					InternalDrawDebugString(hitResult.HitLocation, debugString, Colors.White);

					projectile.TransitionToMedium(newMedium, projectile.SecondsElapsedInMedium - deltaTimeSeconds * (1 - hitResult.Time) + 0.00001f);

					if (!projectile.ShouldBeDestroyed())
						projectile.InvokeOnHit(hitResult);

					continue;
				}
			}

			// Exiting hit
			{
				projectile.RayQueryParameters.From = projectile.GetCurrentWorldLocation();
				projectile.RayQueryParameters.To = projectile.LastWorldLocation;

				var hitResult = PhysicsIntersectionUtility3D.IntersectRay(_rayCast, projectile.RayQueryParameters);
				if (hitResult.IsHit)
				{
					InternalDrawDebugLine(hitResult.RayEnd, hitResult.HitLocation, projectile.Medium.DebugColor);
					InternalDrawDebugPoint(hitResult.HitLocation, Colors.Blue);

					projectile.TransitionToMedium(AirMedium, projectile.SecondsElapsedInMedium - deltaTimeSeconds * hitResult.Time + 0.00001f);

					string debugString = $"Exiting\nBudget: {projectile.PenetrationBudget}\nDamage: {projectile.Damage}";
					InternalDrawDebugString(hitResult.HitLocation, debugString, Colors.White);

					if (!projectile.ShouldBeDestroyed())
						projectile.InvokeOnPenetrationExited(hitResult);

					continue;
				}
			}

			InternalDrawDebugLine(projectile.LastWorldLocation, projectile.GetCurrentWorldLocation(), projectile.Medium.DebugColor);
		}
	}

	private void InternalDrawDebugPoint(Vector3 worldLocation, Color color)
	{
		if (!ShouldDebugDraw)
			return;

		DebugDrawManager.Instance.DrawDebugPoint(worldLocation, color, DebugDrawPrimitiveDuration);
	}

	private void InternalDrawDebugLine(Vector3 worldStart, Vector3 worldEnd, Color color)
	{
		if (!ShouldDebugDraw)
			return;

		DebugDrawManager.Instance.DrawDebugLine(worldStart, worldEnd, color, DebugDrawPrimitiveDuration);
	}


	private void InternalDrawDebugString(Vector3 worldLocation, string str, Color color)
	{
		if (!ShouldDebugDraw)
			return;

		DebugDrawManager.Instance.DrawDebugString(worldLocation, str, color, DebugDrawStringDuration);
	}


}
