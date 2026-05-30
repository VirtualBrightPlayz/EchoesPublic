using System;
using System.Collections.Generic;
using System.IO;
using Godot;

public static class Extensions
{
    public static void QueueFreeNow(this Node node)
    {
        if (GodotObject.IsInstanceValid(node))
        {
            if (GodotObject.IsInstanceValid(node.GetParent()))
                node.GetParent().RemoveChild(node);
            node.QueueFree();
        }
    }

    public static Node GetMultiplayerRoot(this Node node)
    {
        return node. GetNode(((SceneMultiplayer)node.Multiplayer).RootPath);
    }

    public static Vector3 ReadVector3(this BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public static void WriteVector3(this BinaryWriter writer, Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    /// <summary>
    /// Rounds a number to the nearest multiple of another number
    /// Basically GDScript snap
    /// </summary>
    /// <param name="value">The value to round</param>
    /// <param name="factor">The factor to round to a multiple of. Must not be zero.</param>
    /// <param name="mode">Defines direction to round if <paramref name="value"/> is exactly halfway between two multiples of <paramref name="factor"/></param>
    /// <remarks>
    /// Use with caution when <paramref name="value"/> is large or <paramref name="factor"/> is small.
    /// </remarks>
    /// <exception cref="DivideByZeroException">If <paramref name="factor"/> is zero</exception>
    public static double RoundToNearestMultipleOfFactor(this double value, double factor, MidpointRounding mode = MidpointRounding.AwayFromZero)
    {
        return Math.Round(value / factor, mode) * factor;
    }

    /// <summary>
    /// GDScript Indexer.
    /// If the index is negative, it will be treated as an offset from the end of the array.
    /// </summary>
    /// <param name="arr">The array to index.</param>
    /// <param name="index">The index to get.</param>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <returns>The value at the index.</returns>
    public static T GodotIndexer<T>(this IList<T> arr, int index)
    {
        if (index < 0)
            index = arr.Count + index;
        return arr[index];
    }

    /// <summary>
    /// GDScript Indexer but with extra check.
    /// <see cref="GodotIndexer{T}"/>, but if index is <see cref="int.MaxValue"/> it returns <c>default</c>.
    /// </summary>
    /// <param name="arr">The array to index.</param>
    /// <param name="index">The index to get.</param>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <returns>The value at the index, or <c>default</c>.</returns>
    public static T RadialIndexer<T>(this IList<T> arr, int index)
    {
        if (index == int.MaxValue)
            return default;

        return arr.GodotIndexer(index);
    }

    public static void LerpNodePosition(this Node3D node, Vector3 to, float delta)
    {
        node.GlobalPosition = node.GlobalPosition.Lerp(to, delta);
    }

    public static void LerpNodeRotation(this Node3D node, Vector3 to, float delta)
    {
        node.GlobalRotation = node.GlobalRotation.LerpRotation(to, delta);
    }

    public static void LerpNodeRotation(this Node3D node, Basis to, float delta)
    {
        node.GlobalBasis = node.GlobalBasis.Orthonormalized().Slerp(to.Orthonormalized(), delta).Orthonormalized();
    }

    public static Vector3 LerpPosition(this Vector3 from, Vector3 to)
    {
        return from.Lerp(to, (to - from).Length());
    }

    public static Vector3 LerpRotation(this Vector3 from, Vector3 to, float delta)
    {
        Quaternion toQ = Quaternion.FromEuler(to);
        Quaternion fromQ = Quaternion.FromEuler(from);
        return fromQ.Slerp(toQ, delta).GetEuler();
        // return fromQ.Slerp(toQ, Mathf.Abs(fromQ.AngleTo(toQ))).GetEuler();
    }

    public static Vector3 GetInverseValue(Vector3 v)
    {
        return new Vector3(Mathf.IsZeroApprox(v.X) ? 1f : 0f, Mathf.IsZeroApprox(v.Y) ? 1f : 0f, Mathf.IsZeroApprox(v.Z) ? 1f : 0f);
    }

    public static BodyCollision MoveAndSlideRigidBody(this RigidBody3D body, Vector3 move, int maxSteps = 4, float stiffness = 10f, float maxForce = 1f, bool pushBodies = false)
    {
        Vector3 stepMove = move;
        BodyCollision ret = null;
        for (int i = 0; i < maxSteps; i++)
        {
            KinematicCollision3D collision = body.MoveAndCollide(stepMove);
            if (ret == null)
                ret = new BodyCollision();
            if (collision == null)
            {
                ret.travel = move;
                break;
            }
            ret.collider = (Node3D)collision.GetCollider();
            ret.position = collision.GetPosition();
            ret.normal = collision.GetNormal();
            ret.travel = collision.GetTravel();
            Vector3 nextMove = collision.GetRemainder().Slide(collision.GetNormal());

            if (pushBodies)
            {
                if (collision.GetCollider() is RigidBody3D rb)
                {
                    Vector3 lost = stepMove - nextMove;
                    rb.ApplyImpulse((lost * stiffness).LimitLength(maxForce), body.GlobalPosition - rb.GlobalPosition);
                }
            }

            stepMove = nextMove;

            if (nextMove.Dot(move) <= 0f)
                break;
        }
        return ret;
    }

    public static BodyCollision MoveAndSlideCustom(this PhysicsBody3D body, Vector3 move, int maxSteps = 4, float stiffness = 10f, float maxForce = 1f, bool pushBodies = false)
    {
        Vector3 stepMove = move;
        BodyCollision ret = null;
        for (int i = 0; i < maxSteps; i++)
        {
            KinematicCollision3D collision = body.MoveAndCollide(stepMove);
            if (ret == null)
                ret = new BodyCollision();
            if (collision == null)
            {
                ret.travel = move;
                break;
            }
            ret.collider = (Node3D)collision.GetCollider();
            ret.position = collision.GetPosition();
            ret.normal = collision.GetNormal();
            ret.travel = collision.GetTravel();
            Vector3 nextMove = collision.GetRemainder().Slide(collision.GetNormal());

            if (pushBodies)
            {
                if (collision.GetCollider() is RigidBody3D rb)
                {
                    Vector3 lost = stepMove - nextMove;
                    rb.ApplyImpulse((lost * stiffness).LimitLength(maxForce), body.GlobalPosition - rb.GlobalPosition);
                }
            }

            stepMove = nextMove;

            if (nextMove.Dot(move) <= 0f)
                break;
        }
        return ret;
    }

    public static Vector3 MoveAndStairs(this CharacterBody3D body, double delta, Vector3 velocity, Vector3 axis, float dist = 0.5f)
    {
        if (velocity.IsZeroApprox())
            return Vector3.Zero;
        KinematicCollision3D collision = new KinematicCollision3D();
        Transform3D xform = body.GlobalTransform;

        {
            Vector3 vel = velocity * (float)delta;
            // vel = vel - (vel * axis);
            vel = vel.Slide(axis);
            Vector3 travelLimit = vel;
            // check for stairs
            bool hit = body.TestMove(xform, vel, collision, recoveryAsCollision: false, maxCollisions: 1);
            if (hit)
            {
                travelLimit = collision.GetTravel();
                xform.Origin += collision.GetTravel();
                Vector3 remainder = collision.GetRemainder();
                // step up
                hit = body.TestMove(xform, axis * dist, collision, recoveryAsCollision: false, maxCollisions: 1);
                xform.Origin += collision.GetTravel();
                float stepUpDist = collision.GetTravel().Length();
                // move the remaining distance
                hit = body.TestMove(xform, remainder, collision, recoveryAsCollision: false, maxCollisions: 1);
                xform.Origin += collision.GetTravel();
                vel = -axis * stepUpDist;
                // move down to the stairs
                hit = body.TestMove(xform, vel, collision, recoveryAsCollision: false, maxCollisions: 1);
                xform.Origin += collision.GetTravel();
                if (!hit)
                {
                    return Vector3.Zero;
                }
                Vector3 surfNormal = collision.GetNormal();
                if (surfNormal.AngleTo(axis) > body.FloorMaxAngle)
                {
                    return Vector3.Zero;
                }
                return (xform.Origin - body.GlobalPosition) * axis.Abs();
            }
            else
            {
                return Vector3.Zero;
            }
        }
    }
}

public class BodyCollision
{
    public Node3D collider;
    public Vector3 position;
    public Vector3 normal;
    public Vector3 travel;
}