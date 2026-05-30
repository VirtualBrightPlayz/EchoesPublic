using Godot;
using System;

public partial class NoEndoffNeighbourSpawnValidator : SpawnValidator
{
    public override bool IsSpawnValid(Layout layout, LayoutCell cell)
    {
        layout.GetNeighbouringCells(cell, true, out var cellPX, out var cellPZ, out var cellNX, out var cellNZ);

        return (cellPX == null || !RoomDirectionFlagsUtility.IsOneWay(cellPX.Connections)) &&
               (cellPZ == null || !RoomDirectionFlagsUtility.IsOneWay(cellPZ.Connections)) &&
               (cellNX == null || !RoomDirectionFlagsUtility.IsOneWay(cellNX.Connections)) &&
               (cellNZ == null || !RoomDirectionFlagsUtility.IsOneWay(cellNZ.Connections));
    }
}
