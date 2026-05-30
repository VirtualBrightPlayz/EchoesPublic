using Godot;
using System;

public partial class XRoomNeighbourSpawnValidator : SpawnValidator
{
    public override bool IsSpawnValid(Layout layout, LayoutCell cell)
    {
        LayoutCell cellPX;
        LayoutCell cellPZ;
        LayoutCell cellNX;
        LayoutCell cellNZ;
        layout.GetNeighbouringCells(cell, true, out cellPX, out cellPZ, out cellNX, out cellNZ);
        
        return IsCellValid(cellPX) || IsCellValid(cellPZ) || IsCellValid(cellNX) || IsCellValid(cellNZ);
    }

    public bool IsCellValid(LayoutCell cell)
    {
        return cell != null && (!RoomDirectionFlagsUtility.IsOneWay(cell.Connections) &&
                                !RoomDirectionFlagsUtility.IsTwoWayCorner(cell.Connections) &&
                                !RoomDirectionFlagsUtility.IsTwoWayStraight(cell.Connections));
    }
}
