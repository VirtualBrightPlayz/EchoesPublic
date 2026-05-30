using Godot;
using System;
using Godot.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class Layout : Node3D
{
    [Export]
    public Vector2I GridSize { get; private set; }

    [Export]
    public float CellSize { get; private set; }

    public bool IsInitialized { get; private set; }

    public LayoutCell[,] Grid { get; private set; }

    public override void _Ready()
    {
        // Rotating the layout is not supported
        this.Rotation = Vector3.Zero;
    }

    public bool SetLayoutConfig(Vector2I newGridSize, float newCellSize)
    {
        if (this.IsInitialized) return false;

        this.GridSize = newGridSize;
        this.CellSize = newCellSize;
        return true;
    }

    public bool InitializeLayout()
    {
        if (this.IsInitialized) return false;

        this.Grid = new LayoutCell[this.GridSize.X, this.GridSize.Y];
        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                LayoutCell spawnedCell = new LayoutCell(this, new Vector2I(x, y));
                this.AddChild(spawnedCell);
                this.Grid[x, y] = spawnedCell;
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
                LayoutCell cell = this.GetCell(new Vector2I(x, y));
                cell.ResetRoomGenerationResource();
                this.RemoveChild(cell);
                cell.QueueFree();
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
                LayoutCell cell = this.GetCell(new Vector2I(x, y));
                cell.ResetRoomGenerationResource();
            }
        }

        return true;
    }

    public LayoutCell GetCell(Vector2I gridLocation)
    {
        if (gridLocation.X >= 0 && gridLocation.Y >= 0 && gridLocation.X < this.GridSize.X && gridLocation.Y < this.GridSize.Y)
        {
            return this.Grid[gridLocation.X, gridLocation.Y];
        }

        return null;
    }

    public LayoutCell FindCellFromGlobalPositon(Vector3 globalPosition, float toleranceY = -1.0f)
    {
        if (toleranceY >= 0 && (this.GlobalPosition.Y > globalPosition.Y + toleranceY || this.GlobalPosition.Y < globalPosition.Y - toleranceY)) return null;

        Vector3 relativeLocation = globalPosition - this.GlobalPosition - new Vector3(this.CellSize / 2, 0, this.CellSize / 2);
        Vector2I cellLocation = new Vector2I((int)Math.Round(relativeLocation.X / this.CellSize), (int)Math.Round(relativeLocation.Z / this.CellSize));
        return this.GetCell(cellLocation);
    }

    public void GetNeighbouringCells(LayoutCell origin, bool onlyReturnConnectedCells, out LayoutCell cellPX, out LayoutCell cellPZ, out LayoutCell cellNX, out LayoutCell cellNZ)
    {
        if (origin == null)
        {
            cellPX = null;
            cellPZ = null;
            cellNX = null;
            cellNZ = null;
            return;
        }

        cellPX = this.GetCell(new Vector2I(origin.GridPosition.X + 1, origin.GridPosition.Y));
        cellPZ = this.GetCell(new Vector2I(origin.GridPosition.X, origin.GridPosition.Y + 1));
        cellNX = this.GetCell(new Vector2I(origin.GridPosition.X - 1, origin.GridPosition.Y));
        cellNZ = this.GetCell(new Vector2I(origin.GridPosition.X, origin.GridPosition.Y - 1));

        if (onlyReturnConnectedCells)
        {
            cellPX = origin.Connections.HasFlag(ERoomDirectionFlags.PositiveX) ? cellPX : null;
            cellPZ = origin.Connections.HasFlag(ERoomDirectionFlags.PositiveZ) ? cellPZ : null;
            cellNX = origin.Connections.HasFlag(ERoomDirectionFlags.NegativeX) ? cellNX : null;
            cellNZ = origin.Connections.HasFlag(ERoomDirectionFlags.NegativeZ) ? cellNZ : null;
        }
    }

    public LayoutCell FindCellWithRoomGenerationResource(RoomGenerationSettings roomGenerationResource)
    {
        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                LayoutCell cell = this.GetCell(new Vector2I(x, y));
                if (cell.RoomGenerationResource == roomGenerationResource)
                {
                    return cell;
                }
            }
        }

        return null;
    }

    public Array<LayoutCell> FindCellsWithRoomGenerationResource(RoomGenerationSettings roomGenerationResource)
    {
        Array<LayoutCell> results = new Array<LayoutCell>();

        for (int x = 0; x < GridSize.X; x++)
        {
            for (int y = 0; y < GridSize.Y; y++)
            {
                LayoutCell cell = this.GetCell(new Vector2I(x, y));
                if (cell.RoomGenerationResource == roomGenerationResource)
                {
                    results.Add(cell);
                }
            }
        }

        return results;
    }

    // Slow operation
    public bool DoesPathExist(LayoutCell start, LayoutCell goal, Array<LayoutCell> cellsToAvoid)
    {
        if (start == null || goal == null) return false;

        // Cells that have been evaluated already will be ignored, thus we can initialize it with cells to avoid to have them ignored too
        Array<LayoutCell> evaluatedCells = cellsToAvoid.Duplicate();
        Array<LayoutCell> queuedCells = new Array<LayoutCell> { start };

        while (queuedCells.Count > 0) 
        {
            LayoutCell currentCell = queuedCells[0];
            queuedCells.RemoveAt(0);

            if (currentCell == goal) return true;

            LayoutCell cellPX;
            LayoutCell cellPZ;
            LayoutCell cellNX;
            LayoutCell cellNZ;
            this.GetNeighbouringCells(currentCell, true, out cellPX, out cellPZ, out cellNX, out cellNZ);

            if (cellPX != null && !evaluatedCells.Contains(cellPX))
            {
                queuedCells.Add(cellPX);
            }

            if (cellPZ != null && !evaluatedCells.Contains(cellPZ))
            {
                queuedCells.Add(cellPZ);
            }

            if (cellNX != null && !evaluatedCells.Contains(cellNX))
            {
                queuedCells.Add(cellNX);
            }

            if (cellNZ != null && !evaluatedCells.Contains(cellNZ))
            {
                queuedCells.Add(cellNZ);
            }

            evaluatedCells.Add(currentCell);
        }

        return false;
    }

   public List<LayoutCell.SceneLoadData> GetAllSceneLoadData()
   {
        List<LayoutCell.SceneLoadData> outList = new List<LayoutCell.SceneLoadData>();
		for (int x = 0; x < GridSize.X; x++)
		{
			for (int y = 0; y < GridSize.Y; y++)
			{
				LayoutCell cell = this.GetCell(new Vector2I(x, y));
                outList.Add(cell.GetSceneLoadData());
			}
		}

        return outList;
	}
}
