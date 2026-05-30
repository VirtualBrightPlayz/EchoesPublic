using Godot;
using System;

public partial class NoTRoomNeighbourSpawnValidator : SpawnValidator
{
    public override bool IsSpawnValid(Layout layout, LayoutCell cell)
    {
        layout.GetNeighbouringCells(cell, true, out var cellPX, out var cellPZ, out var cellNX, out var cellNZ);

        bool isValid = true;
        int count = 0;
        if (cellPX == null || !RoomDirectionFlagsUtility.IsThreeWay(cellPX.Connections))
            count++;
        if (cellPZ == null || !RoomDirectionFlagsUtility.IsThreeWay(cellPZ.Connections))
            count++;
        if (cellNX == null || !RoomDirectionFlagsUtility.IsThreeWay(cellNX.Connections))
            count++;
        if (cellNZ == null || !RoomDirectionFlagsUtility.IsThreeWay(cellNZ.Connections))
            count++;
        /*
        isValid = isValid && (cellPX != null && !RoomDirectionFlagsUtility.IsThreeWay(cellPX.Connections) || cellPX == null);
        isValid = isValid && (cellPZ != null && !RoomDirectionFlagsUtility.IsThreeWay(cellPZ.Connections) || cellPZ == null);
        isValid = isValid && (cellNX != null && !RoomDirectionFlagsUtility.IsThreeWay(cellNX.Connections) || cellNX == null);
        isValid = isValid && (cellNZ != null && !RoomDirectionFlagsUtility.IsThreeWay(cellNZ.Connections) || cellNZ == null);
        */
        isValid = count < 2;
        return isValid;
    }
}
