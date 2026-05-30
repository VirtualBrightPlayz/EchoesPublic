
using Godot;
using Godot.Collections;
using System;

[Flags]
public enum ERoomDirectionFlags : byte
{
    PositiveX = 1,
    PositiveZ = 2,
    NegativeX = 4,
    NegativeZ = 8,
    AllDirections = PositiveX | PositiveZ | NegativeX | NegativeZ,
}

public static class RoomDirectionFlagsUtility
{
    public static ERoomDirectionFlags RotateRight(ERoomDirectionFlags value)
    {
        int result = (int)value;
        int posX = (result & (int)ERoomDirectionFlags.NegativeZ) >> 3;

        result <<= 1;

        result |= posX;

        result &= (int)ERoomDirectionFlags.AllDirections;

        return (ERoomDirectionFlags)result;
    }

    public static ERoomDirectionFlags RotateLeft(ERoomDirectionFlags value)
    {
        int result = (int)value;
        int negZ = (result & (int)ERoomDirectionFlags.PositiveX) << 3;

        result >>= 1;

        result |= negZ;

        result &= (int)ERoomDirectionFlags.AllDirections;

        return (ERoomDirectionFlags)result;
    }

    public static bool IsOneWay(ERoomDirectionFlags value)
    {
        ERoomDirectionFlags mask = ERoomDirectionFlags.PositiveX;

        for (int i = 0; i < 4; i++)
        {
            if (mask == value) return true;
            mask = RoomDirectionFlagsUtility.RotateRight(mask);
        }

        return false;
    }

    public static bool IsTwoWayStraight(ERoomDirectionFlags value)
    {
        ERoomDirectionFlags mask = ERoomDirectionFlags.PositiveX | ERoomDirectionFlags.NegativeX;

        for (int i = 0; i < 4; i++)
        {
            if (mask == value) return true;
            mask = RoomDirectionFlagsUtility.RotateRight(mask);
        }

        return false;
    }

    public static bool IsTwoWayCorner(ERoomDirectionFlags value)
    {
        ERoomDirectionFlags mask = ERoomDirectionFlags.PositiveX | ERoomDirectionFlags.PositiveZ;

        for (int i = 0; i < 4; i++)
        {
            if (mask == value) return true;
            mask = RoomDirectionFlagsUtility.RotateRight(mask);
        }

        return false;
    }

    public static bool IsThreeWay(ERoomDirectionFlags value)
    {
        ERoomDirectionFlags mask = ERoomDirectionFlags.PositiveX | ERoomDirectionFlags.PositiveZ | ERoomDirectionFlags.NegativeX;

        for (int i = 0; i < 4; i++)
        {
            if (mask == value) return true;
            mask = RoomDirectionFlagsUtility.RotateRight(mask);
        }

        return false;
    }

    public static bool IsFourWay(ERoomDirectionFlags value)
    {
        return value == ERoomDirectionFlags.AllDirections;
    }
}

[GlobalClass]
public partial class RoomGenerationSettings : Resource
{
    /* Diffrent scene variations for more varaity (defaultly chooses one randomly).*/
    [Export]
    public Array<PackedScene> RoomScenes = new Array<PackedScene>();

    /* Minimum instances that will be generated. */
    [Export(PropertyHint.Range, "0,1000,")]
    public int RequiredInstances = 0;

    /* Maximum instances that will be generated. Must be >= 'Required Instances'. */
    [Export(PropertyHint.Range, "-1,1000,")]
    public int MaximumInstances = -1;

    /* How present this row is inside the spawn pool (higher value => higer chance to generate). */
    [Export(PropertyHint.Range, "1,1000,")]
    public int SpawnPoolEntries = 10;

    /* The directions where the room should connect to other rooms. */
    [Export]
    public ERoomDirectionFlags Connections;

    /* The directions where the neighbouring cells are not alloweed to generate. Used if the room goes over the bounds of one cell. Must not conflict with 'Connections'. */
    [Export]
    public ERoomDirectionFlags BlockedNeighbours;

    /* Validates a spawn location before setting the cell to this room. Not all cells have been generated during this time. Return values are AND gated. */
    [Export(PropertyHint.ResourceType, "SpawnValidator")]
    public Array<SpawnValidator> PreSpawnValidatorResources = new Array<SpawnValidator>();

    /* Validates a spawn location after all cells have been generated. Useful for validating if the layout is playable. Return values are AND gated. */
    [Export(PropertyHint.ResourceType, "SpawnValidator")]
    public Array<SpawnValidator> PostSpawnValidatorResources = new Array<SpawnValidator>();

    [Export]
    public bool Test = false;

    [Export]
    public Array<SpawnConstraint> Constraints = new Array<SpawnConstraint>();

    [Export]
    public int MaxConsecutive = -1;

    [Export]
    public int MaxConsecutiveGroup = -1;

    [Export]
    public Array<Vector2I> CellPositions = new Array<Vector2I>();

    [Export]
    public Array<ERoomDirectionFlags> ConnectionDoors = new Array<ERoomDirectionFlags>();

    [Export]
    public Array<ERoomDirectionFlags> BlockedConnectionDoors = new Array<ERoomDirectionFlags>();
}
