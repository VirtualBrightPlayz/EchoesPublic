using Godot;
using System;

public partial class NoCornerNeighbourSpawnValidator : SpawnValidator
{
    public override bool IsSpawnValid(Layout layout, LayoutCell cell)
    {
        layout.GetNeighbouringCells(cell, true, out var cellPX, out var cellPZ, out var cellNX, out var cellNZ);

        return (cellPX == null || !RoomDirectionFlagsUtility.IsTwoWayCorner(cellPX.Connections)) &&
               (cellPZ == null || !RoomDirectionFlagsUtility.IsTwoWayCorner(cellPZ.Connections)) &&
               (cellNX == null || !RoomDirectionFlagsUtility.IsTwoWayCorner(cellNX.Connections)) &&
               (cellNZ == null || !RoomDirectionFlagsUtility.IsTwoWayCorner(cellNZ.Connections));
    }
}
