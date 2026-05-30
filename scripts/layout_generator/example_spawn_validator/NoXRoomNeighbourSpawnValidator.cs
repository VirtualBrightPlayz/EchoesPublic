using Godot;
using System;

public partial class NoXRoomNeighbourSpawnValidator : SpawnValidator
{
    public override bool IsSpawnValid(Layout layout, LayoutCell cell)
    {
        layout.GetNeighbouringCells(cell, true, out var cellPX, out var cellPZ, out var cellNX, out var cellNZ);
        
        return (cellPX == null || !RoomDirectionFlagsUtility.IsFourWay(cellPX.Connections)) &&
               (cellPZ == null || !RoomDirectionFlagsUtility.IsFourWay(cellPZ.Connections)) &&
               (cellNX == null || !RoomDirectionFlagsUtility.IsFourWay(cellNX.Connections)) &&
               (cellNZ == null || !RoomDirectionFlagsUtility.IsFourWay(cellNZ.Connections));
    }
}
