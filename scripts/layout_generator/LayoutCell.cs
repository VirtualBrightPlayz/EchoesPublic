using Godot;
using System;
using Godot.Collections;
using System.Collections.Generic;

public partial class LayoutCell : Node
{
	public struct SceneLoadData
	{
		public string SceneResourcePath;
		public Vector3 GlobalPosition;
		public Vector3 GlobalRotation;
		public ulong UniqueCellSeed;

		public SceneLoadData(string sceneResourcePath, Vector3 globalPostion, Vector3 globalRotation, ulong uniqueCellSeed)
		{
			this.SceneResourcePath = sceneResourcePath;
			this.GlobalPosition = globalPostion;
			this.GlobalRotation = globalRotation;
			this.UniqueCellSeed = uniqueCellSeed;
		}
	}

	public bool IsGenerated { get; private set; }
    public Vector2I GridPosition { get; private set; }
    public int GridRotation { get; private set; }
    public Layout OwningLayout { get; private set; }
    public ulong UniqueCellSeed { get; private set; } // Used for scene selection and unique node seeds
    public RoomGenerationSettings RoomGenerationResource { get; private set; }
    public PackedScene RoomSceneResource { get; private set; }
    public Node3D RoomSceneRootNode { get; private set; }
    public ERoomDirectionFlags Connections { get; private set; }
    public ERoomDirectionFlags BlockedNeighbours { get; private set; }

    public LayoutCell(Layout owningLayout, Vector2I position)
    {
        this.OwningLayout = owningLayout;
        this.GridPosition = position;
        this.Name = $"{owningLayout.Name}_Cell_X{position.X}_Y{position.Y}";
    }

    public ERoomDirectionFlags GetRequiredConnections()
    {
        LayoutCell cellPX;
        LayoutCell cellPZ;
        LayoutCell cellNX;
        LayoutCell cellNZ;
        ERoomDirectionFlags requiredConnections = 0;
        this.OwningLayout.GetNeighbouringCells(this, false, out cellPX, out cellPZ, out cellNX, out cellNZ);

        if (cellPX != null && cellPX.Connections.HasFlag(ERoomDirectionFlags.NegativeX))
        {
            requiredConnections |= ERoomDirectionFlags.PositiveX;
        }

        if (cellPZ != null && cellPZ.Connections.HasFlag(ERoomDirectionFlags.NegativeZ))
        {
            requiredConnections |= ERoomDirectionFlags.PositiveZ;
        }

        if (cellNX != null && cellNX.Connections.HasFlag(ERoomDirectionFlags.PositiveX))
        {
            requiredConnections |= ERoomDirectionFlags.NegativeX;
        }

        if (cellNZ != null && cellNZ.Connections.HasFlag(ERoomDirectionFlags.PositiveZ))
        {
            requiredConnections |= ERoomDirectionFlags.NegativeZ;
        }

        return requiredConnections;
    }

    public ERoomDirectionFlags GetBlockedConnections()
    {
        LayoutCell cellPX;
        LayoutCell cellPZ;
        LayoutCell cellNX;
        LayoutCell cellNZ;
        ERoomDirectionFlags blockedConnections = 0;
        this.OwningLayout.GetNeighbouringCells(this, false, out cellPX, out cellPZ, out cellNX, out cellNZ);

        if (cellPX == null || cellPX.IsBlockedByNeighbour() || (cellPX.IsGenerated && !cellPX.Connections.HasFlag(ERoomDirectionFlags.NegativeX)))
        {
            blockedConnections |= ERoomDirectionFlags.PositiveX;
        }

        if (cellPZ == null || cellPZ.IsBlockedByNeighbour() || (cellPZ.IsGenerated && !cellPZ.Connections.HasFlag(ERoomDirectionFlags.NegativeZ)))
        {
            blockedConnections |= ERoomDirectionFlags.PositiveZ;
        }

        if (cellNX == null || cellNX.IsBlockedByNeighbour() || (cellNX.IsGenerated && !cellNX.Connections.HasFlag(ERoomDirectionFlags.PositiveX)))
        {
            blockedConnections |= ERoomDirectionFlags.NegativeX;
        }

        if (cellNZ == null || cellNZ.IsBlockedByNeighbour() || (cellNZ.IsGenerated && !cellNZ.Connections.HasFlag(ERoomDirectionFlags.PositiveZ)))
        {
            blockedConnections |= ERoomDirectionFlags.NegativeZ;
        }

        return blockedConnections;
    }

    public bool IsBlockedByNeighbour()
    {
        LayoutCell cellPX;
        LayoutCell cellPZ;
        LayoutCell cellNX;
        LayoutCell cellNZ;
        bool isBlocked = false;
        this.OwningLayout.GetNeighbouringCells(this, false, out cellPX, out cellPZ, out cellNX, out cellNZ);

        isBlocked = isBlocked || (cellPX != null && cellPX.BlockedNeighbours.HasFlag(ERoomDirectionFlags.NegativeX));
        isBlocked = isBlocked || (cellPZ != null && cellPZ.BlockedNeighbours.HasFlag(ERoomDirectionFlags.NegativeZ));
        isBlocked = isBlocked || (cellNX != null && cellNX.BlockedNeighbours.HasFlag(ERoomDirectionFlags.PositiveX));
        isBlocked = isBlocked || (cellNZ != null && cellNZ.BlockedNeighbours.HasFlag(ERoomDirectionFlags.PositiveZ));

        return isBlocked;
    }

    public bool IsRequiredToGenerate()
    {
        return this.GetRequiredConnections() != 0;
    }

    // Returs wheter the seed has been set or not. Seed is not being set if this cell already has a valid room resource set.
    public bool SetUniqueCellSeed(ulong newSeed)
    {
        if (this.RoomGenerationResource != null) return false;

        this.UniqueCellSeed = newSeed;
        return true;
    }

    public bool IsRoomGenerationResourceValid(RoomGenerationSettings inRoomGenerationResource, int inRotation)
    {
        if (inRoomGenerationResource == null) return false;

        inRotation = inRotation % 4;

        // Save original properties so we can overwrite them temporarily for the pre spawn validators
        int prevRotation = this.GridRotation;
        RoomGenerationSettings prevRoomGenerationResource = this.RoomGenerationResource;
        ERoomDirectionFlags prevConnections = this.Connections;
        ERoomDirectionFlags prevBlockedNeighbours = this.BlockedNeighbours;

        // Save the rotated values seperately
        ERoomDirectionFlags rotatedConnections = inRoomGenerationResource.Connections;
        ERoomDirectionFlags rotatedBlockedNeighbours = inRoomGenerationResource.BlockedNeighbours;
        for (int i = 0; i < inRotation; i++)
        {
            rotatedConnections = RoomDirectionFlagsUtility.RotateRight(rotatedConnections);
            rotatedBlockedNeighbours = RoomDirectionFlagsUtility.RotateRight(rotatedBlockedNeighbours);
        }

        bool isValid = true;
        if (inRoomGenerationResource.PreSpawnValidatorResources != null && inRoomGenerationResource.PreSpawnValidatorResources.Count > 0)
        {
            // Set values for pre spawn validators
            this.RoomGenerationResource = inRoomGenerationResource;
            this.GridRotation = inRotation;
            this.Connections = rotatedConnections;
            this.BlockedNeighbours = rotatedBlockedNeighbours;

            // Run pre spawn validators
            foreach (SpawnValidator validator in inRoomGenerationResource.PreSpawnValidatorResources)
            {
                isValid = isValid && validator.IsSpawnValid(this.OwningLayout, this);
            }

			// Reset properties to their original state, as later checks require them not being set
			this.RoomGenerationResource = prevRoomGenerationResource;
			this.GridRotation = prevRotation;
            this.Connections = prevConnections;
            this.BlockedNeighbours = prevBlockedNeighbours;
        }

        // Get info from neighbouring cells
        LayoutCell cellPX = null;
        LayoutCell cellPZ = null;
        LayoutCell cellNX = null;
        LayoutCell cellNZ = null;
        ERoomDirectionFlags requiredConnections = this.GetRequiredConnections();
        ERoomDirectionFlags blockedConnections = this.GetBlockedConnections();

        // Only retrive neighbouring cells if we actually need them
        if (rotatedBlockedNeighbours != 0)
        {
            this.OwningLayout.GetNeighbouringCells(this, false, out cellPX, out cellPZ, out cellNX, out cellNZ);
        }

        // Check if all connections fit
        return isValid &&
                  CheckConnectionValidState(ERoomDirectionFlags.PositiveX, requiredConnections, rotatedConnections,
                      rotatedBlockedNeighbours, blockedConnections, cellPX) &&
                  CheckConnectionValidState(ERoomDirectionFlags.PositiveZ, requiredConnections, rotatedConnections,
                      rotatedBlockedNeighbours, blockedConnections, cellPZ) &&
                  CheckConnectionValidState(ERoomDirectionFlags.NegativeX, requiredConnections, rotatedConnections,
                      rotatedBlockedNeighbours, blockedConnections, cellNX) &&
                  CheckConnectionValidState(ERoomDirectionFlags.NegativeZ, requiredConnections, rotatedConnections,
                      rotatedBlockedNeighbours, blockedConnections, cellNZ);
    }

    private static bool CheckConnectionValidState(ERoomDirectionFlags direction, ERoomDirectionFlags requiredDirections,
        ERoomDirectionFlags rotatedDirections, ERoomDirectionFlags rotatedBlockedDirections, ERoomDirectionFlags blockedDirections, in LayoutCell cell)
    {
        return ((requiredDirections.HasFlag(direction) && rotatedDirections.HasFlag(direction)) ||
                !requiredDirections.HasFlag(direction)) &&
               ((blockedDirections.HasFlag(direction) && !rotatedDirections.HasFlag(direction)) ||
                !blockedDirections.HasFlag(direction)) &&
               (!rotatedBlockedDirections.HasFlag(direction) || cell == null ||
                (rotatedBlockedDirections.HasFlag(direction) && !cell.IsGenerated &&
                 !cell.IsRequiredToGenerate() && !cell.IsBlockedByNeighbour()));
    }

    public void SetRoomGenerationResource(RoomGenerationSettings newRoomGenerationResource, int newRotation, int sceneIndex = -1)
    {
        if (newRoomGenerationResource == null)
        {
            this.RoomGenerationResource = null;
            this.RoomSceneResource = null;
            this.GridRotation = 0;
            this.Connections = 0;
            this.BlockedNeighbours = 0;
            this.IsGenerated = true;
            return;
        }

        newRotation = newRotation % 4;

        // Set values
        this.RoomGenerationResource = newRoomGenerationResource;
        this.GridRotation = newRotation;
        this.Connections = newRoomGenerationResource.Connections;
        this.BlockedNeighbours = newRoomGenerationResource.BlockedNeighbours;

        // Rotate values based on rotation
        for (int i = 1; i < newRotation + 1; i++)
        {
            this.Connections = RoomDirectionFlagsUtility.RotateRight(this.Connections);
            this.BlockedNeighbours = RoomDirectionFlagsUtility.RotateRight(this.BlockedNeighbours);
        }

        // Set the scene resource
        if (newRoomGenerationResource.RoomScenes.Count > 0)
        {
            if (sceneIndex == -1)
            {
                RandomNumberGenerator randomStream = new RandomNumberGenerator();
                randomStream.Seed = this.UniqueCellSeed;
                this.RoomSceneResource = newRoomGenerationResource.RoomScenes[randomStream.RandiRange(0, newRoomGenerationResource.RoomScenes.Count - 1)];
            }
            else
            {
                this.RoomSceneResource = newRoomGenerationResource.RoomScenes[sceneIndex];
            }
        }
        else
        {
            this.RoomSceneResource = null;
        }

        this.IsGenerated = true;
    }

    public void ResetRoomGenerationResource()
    {
        this.RoomGenerationResource = null;
        this.RoomSceneResource = null;
        this.GridRotation = 0;
        this.Connections = 0;
        this.BlockedNeighbours = 0;
        this.IsGenerated = false;
        this.UniqueCellSeed = 0;
    }

    public Vector3[] GetGlobalDoorPositions()
    {
        Vector3 pos = GetGlobalPosition();
        Vector3 centerOffset = new Vector3(this.OwningLayout.CellSize / 2, 0, this.OwningLayout.CellSize / 2);
        List<Vector3> points = new List<Vector3>();
        if (Connections.HasFlag(ERoomDirectionFlags.PositiveZ))
        {
            points.Add(pos + new Vector3(0, 0, OwningLayout.CellSize / 2));
        }
        if (Connections.HasFlag(ERoomDirectionFlags.PositiveX))
        {
            points.Add(pos + new Vector3(OwningLayout.CellSize / 2, 0, 0));
        }
        /*
        if (Connections.HasFlag(ERoomDirectionFlags.NegativeZ))
        {
            points.Add(pos - new Vector3(0, 0, OwningLayout.CellSize / 2));
        }
        if (Connections.HasFlag(ERoomDirectionFlags.NegativeX))
        {
            points.Add(pos - new Vector3(OwningLayout.CellSize / 2, 0, 0));
        }
        */
        return points.ToArray();
    }

    public Vector3[] GetGlobalDoorRotations()
    {
        Vector3 rot = GetGlobalRotation();
        rot = Vector3.Zero;
        List<Vector3> points = new List<Vector3>();
        if (Connections.HasFlag(ERoomDirectionFlags.PositiveZ))
        {
            points.Add(rot + new Vector3(0, 0, 0));
        }
        if (Connections.HasFlag(ERoomDirectionFlags.PositiveX))
        {
            points.Add(rot + new Vector3(0, Mathf.DegToRad(270), 0));
        }
        /*
        if (Connections.HasFlag(ERoomDirectionFlags.NegativeZ))
        {
            points.Add(rot + new Vector3(0, Mathf.DegToRad(180), 0));
        }
        if (Connections.HasFlag(ERoomDirectionFlags.NegativeX))
        {
            points.Add(rot + new Vector3(0, Mathf.DegToRad(90), 0));
        }
        */
        return points.ToArray();
    }

    public Vector3 GetGlobalPosition()
    {
        Vector3 basePosition = new Vector3(this.GridPosition.X, 0, this.GridPosition.Y) * this.OwningLayout.CellSize;
        Vector3 centerOffset = new Vector3(this.OwningLayout.CellSize / 2, 0, this.OwningLayout.CellSize / 2);
        return basePosition + centerOffset + this.OwningLayout.GlobalPosition;
	}

    public Vector3 GetGlobalRotation()
    {
        return new Vector3(0, (float)(this.GridRotation * -0.5f * Math.PI), 0);
    }

    public Vector3 GetGlobalRotationDegrees()
    {
        return this.GetGlobalRotation() * (float)(Math.PI / 180.0f);
    }

    public SceneLoadData GetSceneLoadData()
    {
        return new SceneLoadData(this.RoomSceneResource.ResourcePath, this.GetGlobalPosition(), this.GetGlobalRotation(), this.UniqueCellSeed);
    }
}
