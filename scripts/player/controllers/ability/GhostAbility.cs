using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class GhostAbility : BaseAbility
{
    [Export] public Area3D area;
    [Export] public float DoorSpeedMultiplier = 0.5f;
    [Export] public float PropSpeedMultiplier = 0.5f;

    public DoorButton foundDoor = null;
    public PhysicsProp3D foundProp = null;

    public static StringName door_106 = "door_106";

    public override void SetupFromDefinition()
    {
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "door_speed_multiplier", ref DoorSpeedMultiplier);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "prop_speed_multiplier", ref PropSpeedMultiplier);
    }

    public override void _Ready()
    {
        base._Ready();
        area.BodyEntered += BodyEnter;
        area.BodyExited += BodyExit;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        area.GlobalTransform = Player.ActiveController.Root.GlobalTransform;
    }


    private void UpdateExtraSpeed()
    {
        if (IsMultiplayerAuthority() && Player.ActiveController is FPController fp)
        {
            float val = 1f;
            if (IsInstanceValid(foundDoor))
            {
                val *= DoorSpeedMultiplier;
            }
            if (IsInstanceValid(foundProp))
            {
                val *= PropSpeedMultiplier;
            }
            fp.ExtraSpeed = val;
        }
    }

    private void BodyEnter(Node3D body)
    {
        if (Player.ActiveController is PhysicsBody3D pb)
        {
            if (body is DoorButton btn)
            {
                if (DoorSpeedMultiplier <= 0d)
                    return;
                if (IsInstanceValid(btn.door) && btn.door.isLocked && !btn.door.HasMeta(door_106))
                    return;
                if (IsInstanceValid(btn.door) && btn.door.isOpen && !btn.door.isMoving)
                    return;
                if (body.HasMeta(door_106) && !body.GetMeta(door_106, false).AsBool())
                    return;
                PhysicsServer3D.BodyAddCollisionException(pb.GetRid(), btn.GetRid());
                foundDoor = btn;
            }
            else if (body is PhysicsProp3D prop)
            {
                if (PropSpeedMultiplier <= 0d)
                    return;
                if (!prop.Scp106CanPassThrough)
                    return;
                PhysicsServer3D.BodyAddCollisionException(pb.GetRid(), prop.GetRid());
                foundProp = prop;
            }
            UpdateExtraSpeed();
        }
    }

    private void BodyExit(Node3D body)
    {
        if (body == foundDoor)
        {
            foundDoor = null;
        }
        if (body == foundProp)
        {
            foundProp = null;
        }
        UpdateExtraSpeed();
    }
}
