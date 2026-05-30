using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class ZoneArea : Area3D
{
    public enum Zone
    {
        Unknown = 0,
        LightContainment = 1,
        HeavyContainment = 2,
        Entrance = 3,
        Surface = 4,
        Brig = 5,
    }

    public static StringName GroupName = "zone_area";

    public List<IPlayerController> controllersInZone = new List<IPlayerController>();

    [Export]
    public Zone zone;

    public override void _EnterTree()
    {
        base._EnterTree();
        CollisionLayer = 32;
        CollisionMask = 6;
        Monitoring = true;
        Monitorable = false;
    }

    public override void _Ready()
    {
        base._Ready();
        AddToGroup(GroupName);
        BodyEntered += _Entered;
        BodyExited += _Exited;
    }

    private void _Entered(Node3D body)
    {
        if (body is IPlayerController player)
        {
            controllersInZone.Add(player);
        }
    }

    private void _Exited(Node3D body)
    {
        if (body is IPlayerController player)
        {
            controllersInZone.Remove(player);
        }
    }

    public static Zone GetZone(Vector3 position)
    {
        var args = new PhysicsPointQueryParameters3D()
        {
            CollideWithAreas = true,
            CollideWithBodies = false,
            Position = position,
            CollisionMask = uint.MaxValue,
        };
        if (IsInstanceValid(ItemManager.Instance))
        {
            if (!IsInstanceValid(ItemManager.Instance.GetWorld3D().DirectSpaceState))
                throw new Exception("Use only in physics process.");
            var results = ItemManager.Instance.GetWorld3D().DirectSpaceState.IntersectPoint(args, 64);
            foreach (var item in results)
            {
                if (item["collider"].As<Node>() is ZoneArea area)
                {
                    return area.zone;
                }
            }
        }
        return Zone.Unknown;
    }

    public static ZoneArea GetZoneArea(Vector3 position)
    {
        var args = new PhysicsPointQueryParameters3D()
        {
            CollideWithAreas = true,
            CollideWithBodies = false,
            Position = position,
            CollisionMask = uint.MaxValue,
        };
        if (IsInstanceValid(ItemManager.Instance))
        {
            if (!IsInstanceValid(ItemManager.Instance.GetWorld3D().DirectSpaceState))
                throw new Exception("Use only in physics process.");
            var results = ItemManager.Instance.GetWorld3D().DirectSpaceState.IntersectPoint(args, 64);
            foreach (var item in results)
            {
                if (item["collider"].As<Node>() is ZoneArea area)
                {
                    return area;
                }
            }
        }
        return null;
    }

    public bool IsPlayerInZone(NetworkPlayer player)
    {
        // return OverlapsBody(player.controller);
        if (controllersInZone.Contains(player.ActiveController))
            return true;
        return false;
        bool found = false;
        foreach (var body in GetOverlappingBodies())
        {
            if (body == player)
            {
                found = true;
                break;
            }
            if (player.IsAncestorOf(body))
            {
                found = true;
                break;
            }
        }
        foreach (var body in GetOverlappingAreas())
        {
            if (player.IsAncestorOf(body))
            {
                found = true;
                break;
            }
        }
        return found;
    }
}