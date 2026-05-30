using Godot;
using Godot.NativeInterop;
using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class PhysicsIntersectionUtility3D
{
	public struct HitResult
	{
		public HitResult() { }

		// Directly dictated by native dictionary
		public bool IsHit { get; set; } = false;
		public Vector3 HitLocation { get; set; } = Vector3.Zero;
		public Vector3 HitNormal { get; set; } = Vector3.Zero;
		public long FaceIndex { get; set; } = -1;
		public Node3D Collider { get; set; } = null;
		public Rid ColliderId { get; set; } = default;
		public long ShapeIndex { get; set; } = -1;

		// Calculated extra features
		public Vector3 RayStart { get; set; } = Vector3.Zero;
		public Vector3 RayEnd { get; set; } = Vector3.Zero;
		public double Distance { get; set; } = 0f;
		public double Time { get; set; } = 1f;

	}

	private static IntPtr IntersectRayMethodPtr = 0;

	private static Variant ColliderStringNameVariant = "collider";
	private static Variant ColliderIdStringNameVariant = "collider_id";
	private static Variant HitNormalStringNameVariant = "normal";
	private static Variant HitLocationStringNameVariant = "position";
	private static Variant FaceIndexNameVariant = "face_index";
	private static Variant ShapeIndexNameVariant = "shape";

	static PhysicsIntersectionUtility3D()
	{
		Type spaceStateType = typeof(PhysicsDirectSpaceState3D);
		FieldInfo fieldInfo = spaceStateType.GetField("MethodBind1", BindingFlags.NonPublic | BindingFlags.Static);
		IntersectRayMethodPtr = (IntPtr)fieldInfo.GetValue(null);

		Debug.Assert(IntersectRayMethodPtr != 0);
	}

	public static HitResult IntersectRay(RayCast3D cast, PhysicsRayQueryParameters3D queryParameters, bool shouldDrawDebug = false, double drawDebugDurationSeconds = 3f)
	{
		if (cast == null || queryParameters == null)
			throw new ArgumentNullException();
		cast.GlobalPosition = queryParameters.From;
		cast.TargetPosition = cast.ToLocal(queryParameters.To);
		cast.CollisionMask = queryParameters.CollisionMask;
		cast.ClearExceptions();
		foreach (var exc in queryParameters.Exclude)
		{
			cast.AddExceptionRid(exc);
		}
		cast.CollideWithBodies = queryParameters.CollideWithBodies;
		cast.CollideWithAreas = queryParameters.CollideWithAreas;
		cast.HitFromInside = queryParameters.HitFromInside;
		cast.HitBackFaces = queryParameters.HitBackFaces;
		cast.ForceRaycastUpdate();

		HitResult result = new HitResult();
		if (cast.IsColliding())
		{
			result.IsHit = true;
			result.HitLocation = cast.GetCollisionPoint();
			result.HitNormal = cast.GetCollisionNormal();
			result.FaceIndex = cast.GetCollisionFaceIndex();
			result.Collider = cast.GetCollider() as Node3D;
			result.ColliderId = cast.GetColliderRid();
			result.ShapeIndex = cast.GetColliderShape();
		}

		result.RayStart = queryParameters.From;
		result.RayEnd = queryParameters.To;
		double rayLength = (result.RayEnd - result.RayStart).Length();

		if (result.IsHit)
			result.Distance = (result.HitLocation - result.RayStart).Length();
		else
			result.Distance = rayLength;

		result.Time = result.Distance / rayLength;

		if (shouldDrawDebug)
		{
			DebugDrawManager.Instance.DrawDebugLine(result.RayStart, result.HitLocation, Colors.Green, drawDebugDurationSeconds);
			DebugDrawManager.Instance.DrawDebugLine(result.HitLocation, result.RayEnd, Colors.Red, drawDebugDurationSeconds);
			DebugDrawManager.Instance.DrawDebugPoint(result.HitLocation, Colors.Red, drawDebugDurationSeconds);
		}
		return result;
	}

	public static HitResult IntersectRay(PhysicsDirectSpaceState3D spaceState, PhysicsRayQueryParameters3D queryParameters, bool shouldDrawDebug = false, double drawDebugDurationSeconds = 3f)
	{
		throw new NotImplementedException();
		if (spaceState == null || queryParameters == null)
			throw new ArgumentNullException();

		godot_dictionary nativeResultDictionary = default;

		unsafe
		{
			nint spaceStatePtr = spaceState.NativeInstance;
			nint queryParametersPtr = queryParameters.NativeInstance;

			void** nativeArguments = stackalloc void*[1];
			nativeArguments[0] = &queryParametersPtr;

			NativeFuncs.godotsharp_method_bind_ptrcall(IntersectRayMethodPtr, spaceStatePtr, nativeArguments, &nativeResultDictionary);
		}

		HitResult hitResult = InternalConvertNativeDictionaryToHitResult(ref nativeResultDictionary);

		hitResult.RayStart = queryParameters.From;
		hitResult.RayEnd = queryParameters.To;
		double rayLength = (hitResult.RayEnd - hitResult.RayStart).Length();

		if (hitResult.IsHit)
			hitResult.Distance = (hitResult.HitLocation - hitResult.RayStart).Length();
		else
			hitResult.Distance = rayLength;

		hitResult.Time = hitResult.Distance / rayLength;

		if (shouldDrawDebug)
		{
			DebugDrawManager.Instance.DrawDebugLine(hitResult.RayStart, hitResult.HitLocation, Colors.Green, drawDebugDurationSeconds);
			DebugDrawManager.Instance.DrawDebugLine(hitResult.HitLocation, hitResult.RayEnd, Colors.Red, drawDebugDurationSeconds);
			DebugDrawManager.Instance.DrawDebugPoint(hitResult.HitLocation, Colors.Red, drawDebugDurationSeconds);
		}

		NativeFuncs.godotsharp_dictionary_destroy(ref nativeResultDictionary);
		return hitResult;
	}

	private static HitResult InternalConvertNativeDictionaryToHitResult(ref godot_dictionary dictionary)
	{
		HitResult hitResult = new();
		if (!dictionary.IsAllocated)
			return hitResult;

		if (NativeFuncs.godotsharp_dictionary_count(ref dictionary) == 0)
			return hitResult;

		hitResult.IsHit = true;

		godot_variant nativeKey = default;
		godot_variant nativeValue = default;

		// Collider
		{
			nativeKey = ColliderStringNameVariant.CopyNativeVariant();

			NativeFuncs.godotsharp_dictionary_try_get_value(ref dictionary, nativeKey, out nativeValue);

			Rid rid = NativeFuncs.godotsharp_variant_as_rid(nativeValue);
			if (rid.IsValid)
			{
				ulong instanceId = PhysicsServer3D.BodyGetObjectInstanceId(rid);
				if (instanceId != 0)
					hitResult.Collider = GodotObject.InstanceFromId(instanceId) as Node3D;
			}

			NativeFuncs.godotsharp_variant_destroy(ref nativeValue);
			NativeFuncs.godotsharp_variant_destroy(ref nativeKey);
		}

		// Collider Id
		{
			nativeKey = ColliderIdStringNameVariant.CopyNativeVariant();

			NativeFuncs.godotsharp_dictionary_try_get_value(ref dictionary, nativeKey, out nativeValue);

			// hitResult.ColliderId = NativeFuncs.godotsharp_variant_as_int(nativeValue);

			NativeFuncs.godotsharp_variant_destroy(ref nativeValue);
			NativeFuncs.godotsharp_variant_destroy(ref nativeKey);
		}

		// Hit Normal
		{
			nativeKey = HitNormalStringNameVariant.CopyNativeVariant();

			NativeFuncs.godotsharp_dictionary_try_get_value(ref dictionary, nativeKey, out nativeValue);
			hitResult.HitNormal = NativeFuncs.godotsharp_variant_as_vector3(nativeValue);

			NativeFuncs.godotsharp_variant_destroy(ref nativeValue);
			NativeFuncs.godotsharp_variant_destroy(ref nativeKey);
		}

		// Hit Location
		{
			nativeKey = HitLocationStringNameVariant.CopyNativeVariant();

			NativeFuncs.godotsharp_dictionary_try_get_value(ref dictionary, nativeKey, out nativeValue);
			hitResult.HitLocation = NativeFuncs.godotsharp_variant_as_vector3(nativeValue);

			NativeFuncs.godotsharp_variant_destroy(ref nativeValue);
			NativeFuncs.godotsharp_variant_destroy(ref nativeKey);
		}

		// Face Index
		{
			nativeKey = FaceIndexNameVariant.CopyNativeVariant();

			NativeFuncs.godotsharp_dictionary_try_get_value(ref dictionary, nativeKey, out nativeValue);

			hitResult.FaceIndex = NativeFuncs.godotsharp_variant_as_int(nativeValue);

			NativeFuncs.godotsharp_variant_destroy(ref nativeValue);
			NativeFuncs.godotsharp_variant_destroy(ref nativeKey);
		}

		// Shape Index
		{
			nativeKey = ShapeIndexNameVariant.CopyNativeVariant();

			NativeFuncs.godotsharp_dictionary_try_get_value(ref dictionary, nativeKey, out nativeValue);

			hitResult.ShapeIndex = NativeFuncs.godotsharp_variant_as_int(nativeValue);

			NativeFuncs.godotsharp_variant_destroy(ref nativeValue);
			NativeFuncs.godotsharp_variant_destroy(ref nativeKey);
		}

		return hitResult;
	}
}
