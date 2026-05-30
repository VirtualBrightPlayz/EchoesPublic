using Godot;
using System;
using Godot.Collections;
using System.Collections.Generic;

public partial class LayoutGrid3D : Node3D
{
    [Export]
    public Vector3I GridSize { get; private set; }

    [Export]
    public float CellSize { get; private set; }

    public bool IsInitialized { get; private set; }

    public LayoutCell3D[,,] Grid { get; private set; }

    public override void _Ready()
    {
        // Rotating the layout is not supported
        this.Rotation = Vector3.Zero;
    }

    public bool InitializeLayout()
    {
        if (this.IsInitialized) return false;

        this.Grid = new LayoutCell3D[this.GridSize.X, this.GridSize.Y, this.GridSize.Z];
        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                for (int z = 0; z < GridSize.Z; z++)
                {
                    LayoutCell3D spawnedCell = new LayoutCell3D(this, new Vector3I(x, y, z));
                    this.AddChild(spawnedCell);
                    this.Grid[x, y, z] = spawnedCell;
                }
            }
        }

        this.IsInitialized = true;
        Log.Print($"{this.Name}: Successfully created new layout");
        return true;
    }

    public bool ClearLayout()
    {
        if (!this.IsInitialized) return false;

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                for (int z = 0; z < GridSize.Z; z++)
                {
                    LayoutCell3D cell = this.GetCell(new Vector3I(x, y, z));
                    this.RemoveChild(cell);
                    cell.QueueFree();
                }
            }
        }

        this.Grid = null;
        this.IsInitialized = false;
        Log.Print($"{this.Name}: Successfully cleared layout");
        return true;
    }

    public bool ResetLayout()
    {
        if (!this.IsInitialized) return false;

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                for (int z = 0; z < GridSize.Z; z++)
                {
                    LayoutCell3D cell = this.GetCell(new Vector3I(x, y, z));
                }
            }
        }

        return true;
    }

    public LayoutCell3D GetCell(Vector3I gridLocation)
    {
        if (gridLocation.X >= 0 && gridLocation.Y >= 0 && gridLocation.X < this.GridSize.X && gridLocation.Y < this.GridSize.Y && gridLocation.Z >= 0 && gridLocation.Z < this.GridSize.Z)
        {
            return this.Grid[gridLocation.X, gridLocation.Y, gridLocation.Z];
        }

        return null;
    }

    public LayoutCell3D FindCellFromGlobalPositon(Vector3 globalPosition, float toleranceY = -1.0f)
    {
        if (toleranceY >= 0 && (this.GlobalPosition.Y > globalPosition.Y + toleranceY || this.GlobalPosition.Y < globalPosition.Y - toleranceY)) return null;

        Vector3 relativeLocation = globalPosition - this.GlobalPosition - new Vector3(this.CellSize / 2, 0, this.CellSize / 2);
        Vector3I cellLocation = new Vector3I((int)Math.Round(relativeLocation.X / this.CellSize), (int)Math.Round(relativeLocation.Y / this.CellSize), (int)Math.Round(relativeLocation.Z / this.CellSize));
        return this.GetCell(cellLocation);
    }
}
