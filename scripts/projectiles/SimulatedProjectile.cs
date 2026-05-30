using Godot;
using System;
using static PhysicsIntersectionUtility3D;

public class SimulatedProjectile
{
	public delegate void OnHitHandler(SimulatedProjectile projectile, HitResult hitResult);
	public delegate void OnPenetrationExitedHandler(SimulatedProjectile projectile, HitResult hitResult);

	public SimulatedProjectile(Vector3 initialWorldLocation, Vector3 initialVelocity, double penetrationBudget, double damage)
	{
		GravityMultiplier = 1f;
		RayQueryParameters = PhysicsRayQueryParameters3D.Create(Vector3.Zero, Vector3.Zero);
		RayQueryParameters.HitFromInside = false;
		RayQueryParameters.HitBackFaces = false;

		InitialWorldLocation = initialWorldLocation;
		InitialVelocity = initialVelocity;

		PenetrationBudget = penetrationBudget;
		Damage = damage;
		TotalTimeElapsedSeconds = 0f;

		LastWorldLocation = initialWorldLocation;

		InternalInitializeMedium(ProjectileSimulationManager.Instance.AirMedium);
	}

	// Initial parameters across medium
	public double GravityMultiplier { get; set; }
	public PhysicsRayQueryParameters3D RayQueryParameters { get; set; }
	public event OnHitHandler OnHit;
	public event OnPenetrationExitedHandler OnPenetrationExited;

	// Initial parameters in medium
	public Vector3 InitialWorldLocation { get; private set; }
	public Vector3 InitialVelocity { get; private set; }
	public ProjectileResistanceMedium Medium { get; private set; }

	// Changing parameters in medium
	public double SecondsElapsedInMedium { get; private set; }

	// Changing parameters across medium
	public double PenetrationBudget { get; set; }
	public double Damage { get; set; }
	public double TotalTimeElapsedSeconds { get; private set; }
	public Vector3 LastWorldLocation { get; private set; }


	public Vector3 GetCurrentWorldLocation()
	{
		return InternalGetWorldLocationAtTime(SecondsElapsedInMedium);
	}

	public Vector3 GetCurrentVelocity()
	{
		return InternalGetVelocityAtTime(SecondsElapsedInMedium);
	}

	public void StepSimulation(double deltaTimeSeconds)
	{
		LastWorldLocation = GetCurrentWorldLocation();

		SecondsElapsedInMedium += deltaTimeSeconds;
		TotalTimeElapsedSeconds += deltaTimeSeconds;
	}
	private void InternalInitializeMedium(ProjectileResistanceMedium medium)
	{
		Medium = medium;
		SecondsElapsedInMedium = 0f;
	}

	public void TransitionToMedium(ProjectileResistanceMedium newMedium, double transitionTimePointSeconds)
	{
		double distanceTraveledInMediumCm = GetCurrentVelocity().Length() * 100f * transitionTimePointSeconds;
		PenetrationBudget -= Medium.PenetrationCost * distanceTraveledInMediumCm;
		Damage *= Math.Exp(Math.Log(Medium.DamageMultiplier) * distanceTraveledInMediumCm);

		Vector3 newLocation = InternalGetWorldLocationAtTime(transitionTimePointSeconds);
		Vector3 newVelocity = InternalGetVelocityAtTime(transitionTimePointSeconds);

		InitialWorldLocation = newLocation;
		InitialVelocity = newVelocity * (float)newMedium.SpeedMultiplier;

		InternalInitializeMedium(newMedium);
	}

	private Vector3 InternalGetWorldLocationAtTime(double time)
	{
		double localLocationY = InitialVelocity.Y * time - 0.5 * GetEffectiveGravityAcceleration() * (time * time);

		Vector3 worldLocation = InitialWorldLocation;
		worldLocation.Y += (float)localLocationY;
		worldLocation.X += InitialVelocity.X * (float)time;
		worldLocation.Z += InitialVelocity.Z * (float)time;

		return worldLocation;
	}

	private Vector3 InternalGetVelocityAtTime(double time)
	{
		double localVelocityY = InitialVelocity.Y - GetEffectiveGravityAcceleration() * time;
		return new Vector3(InitialVelocity.X, (float)localVelocityY, InitialVelocity.Z);
	}

	public double GetEffectiveGravityAcceleration()
	{
		return ProjectileSimulationManager.Instance.GravityAcceleration * GravityMultiplier * Medium.GravityMultiplier;
	}

	public bool ShouldBeDestroyed()
	{
		if (PenetrationBudget - (Medium.PenetrationCost * 100f * SecondsElapsedInMedium) < 0)
			return true;

		if (TotalTimeElapsedSeconds > ProjectileSimulationManager.MAX_PROJECTILE_LIFETIME_SECONDS)
			return true;

		return false;
	}

	public void Invalidate()
	{
		PenetrationBudget = float.NegativeInfinity;
	}

	public void InvokeOnHit(HitResult hitResult)
	{
		OnHit?.Invoke(this, hitResult);
	}

	public void InvokeOnPenetrationExited(HitResult hitResult)
	{
		OnPenetrationExited?.Invoke(this, hitResult);
	}
}