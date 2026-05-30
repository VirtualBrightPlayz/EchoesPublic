using Godot;

// https://www.youtube.com/watch?v=Y3MgFS-9l3s
[GlobalClass]
public partial class PIDController : Node3D
{
    [Export] public RigidBody3D target;
    [Export] public Vector3 grabPositionLocal;
    [Export] public Basis grabRotation;
    [Export] public float maxForce = 100f;
    [Export] public float Kp = 1f;
    [Export] public float Ki = 1f;
    [Export] public float Kd = 1f;
    [Export] public float maxRotationForce = 100f;
    [Export] public float KRp = 1f;
    [Export] public float KRi = 1f;
    [Export] public float KRd = 1f;

    public Vector3 integralPosition;
    public Vector3? lastErrorPosition;

    public Vector3 integralRotation;
    public Vector3? lastErrorRotation;
    public Quaternion? lastTo;

    public void SetTarget(RigidBody3D rb, Vector3 localPos, Vector3 fromPos)
    {
        target = rb;
        lastErrorPosition = null;
        integralPosition = Vector3.Zero;
        lastErrorRotation = null;
        integralRotation = Vector3.Zero;
        grabPositionLocal = localPos;
        lastTo = null;
        if (IsInstanceValid(target))
            grabRotation = target.GlobalBasis.Inverse() * Basis.LookingAt(fromPos.DirectionTo(target.ToGlobal(grabPositionLocal)));
            // grabRotation = Basis.LookingAt(target.ToLocal(fromPos).DirectionTo(grabPositionLocal));
    }

    public void SetTargetB(RigidBody3D rb, Vector3 localPos, Basis fromBasisWorld)
    {
        target = rb;
        lastErrorPosition = null;
        integralPosition = Vector3.Zero;
        lastErrorRotation = null;
        integralRotation = Vector3.Zero;
        grabPositionLocal = localPos;
        lastTo = null;
        if (IsInstanceValid(target))
            grabRotation = target.GlobalBasis.Inverse() * fromBasisWorld.Orthonormalized();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(target))
        {
            ProcessGrabbedPosition(delta);
            for (int i = 0; i < 1; i++)
                ProcessGrabbedRotation(delta, Vector3.One, ref integralRotation, ref lastErrorRotation);
        }
    }

    public void ProcessGrabbedPosition(double delta)
    {
        if (Mathf.IsZeroApprox(delta))
            return;
        Vector3 error = GlobalPosition - target.ToGlobal(grabPositionLocal);
        Vector3 error_d = (error - lastErrorPosition.GetValueOrDefault(error)) / (float)delta;
        integralPosition = (integralPosition + error * (float)delta).LimitLength(maxForce);
        lastErrorPosition = error;
        var p = error * Kp;
        var i = integralPosition * Ki;
        var d = error_d * Kd;
        var final = p + i + d;
        target.ApplyForce(p, target.ToGlobal(grabPositionLocal) - target.GlobalPosition);
        target.ApplyCentralForce(i + d);
    }

    public Vector3 EulerAngleDiff(Vector3 a, Vector3 b)
    {
        float min = -Mathf.Pi;
        float max = Mathf.Pi;
        Vector3 error = a - b;
        error.X = Mathf.Wrap(error.X, min, max);
        error.Y = Mathf.Wrap(error.Y, min, max);
        error.Z = Mathf.Wrap(error.Z, min, max);
        return error;
    }

    public Vector3 AngleDiff(Quaternion a, Quaternion b)
    {
        Quaternion error = (a * b.Inverse()).Normalized();
        if (error.W < 0)
        {
            error.W = -error.W;
        }
        return error.GetAxis() * error.GetAngle();
    }

    public Vector3 AxisAngle(Quaternion a)
    {
        Quaternion error = a;
        if (error.W < 0)
        {
            // error.W = -error.W;
        }
        float angle = error.GetAngle();
        Vector3 axis = error.GetAxis();
        if (angle >= Mathf.Pi)
        {
            // axis = -axis;
            // angle = Mathf.Pi - angle;
        }
        return axis * Mathf.Wrap(angle, -Mathf.Pi, Mathf.Pi);
    }

    public void ProcessGrabbedRotation(double delta, Vector3 axis, ref Vector3 integral, ref Vector3? lastError)
    {
        if (Mathf.IsZeroApprox(delta))
            return;
        // Basis fromRot = (target.GlobalBasis * grabRotation).Orthonormalized();
        var fromRot = new Quaternion(target.GlobalBasis * grabRotation).Normalized();
        var toRot = new Quaternion(GlobalBasis).Normalized();
        var error_b = (toRot * fromRot.Inverse()).Normalized();
        // var error_e = EulerAngleDiff(GlobalRotation, (target.GlobalBasis * grabRotation).GetEuler());
        // {
            // GD.PrintErr($"{(fromRot * error_b).AngleTo(toRot)}");
        // }
        Vector3 error = AxisAngle(error_b);
        // error = error_e;
        // Vector3 error = AxisAngle((fromRot.Inverse() * toRot).Normalized());
        Vector3 error_d = (error - lastError.GetValueOrDefault(error)) / (float)delta;
        integral = (integral + (error * (float)delta)).LimitLength(maxForce);
        lastError = error;
        // Basis error = (fromRot.Inverse() * toRot);
        // Quaternion error = (toRot.Inverse() * fromRot).Normalized();
        // Basis error_d = (error * lastError.GetValueOrDefault(error).Inverse()).Normalized();
        // Vector3 error = AngleDiff(GlobalBasis.Orthonormalized().GetRotationQuaternion(), to);
        // Vector3 error_d = EulerAngleDiff(error, lastError.GetValueOrDefault(error)) * axis / (float)delta;
        // Vector3 error_d = -AngleDiff(to, lastTo.GetValueOrDefault());
        if (!lastError.HasValue)
        {
            // error_d = Basis.Identity;
        }
        error_d = -target.AngularVelocity;
        // lastTo = toRot;
        // integral = (integral + (error * (float)delta));//.LimitLength(maxRotationForce);
        var p = error * (KRp);
        var i = integral * (KRi);
        var d = error_d * (KRd);
        target.ApplyTorque((p + i + d).LimitLength(maxRotationForce) * PhysicsServer3D.BodyGetDirectState(target.GetRid()).InverseInertia.Inverse().Clamp(-1f, 1f));
        // target.ApplyTorque(p + i + d);
        // target.AngularVelocity = Vector3.Zero;
        // DebugDrawManager.Instance.DrawDebugString(GlobalPosition, $"", Colors.White);
        // DebugDrawManager.Instance.DrawDebugLine(GlobalPosition, GlobalPosition + p + i + d, Colors.White, delta);
        // DebugDrawManager.Instance.DrawDebugLine(GlobalPosition, GlobalPosition + p, Colors.Red, delta);
        // DebugDrawManager.Instance.DrawDebugLine(GlobalPosition, GlobalPosition + i, Colors.Green, delta);
        // DebugDrawManager.Instance.DrawDebugLine(GlobalPosition, GlobalPosition + d, Colors.Blue, delta);
    }
}
