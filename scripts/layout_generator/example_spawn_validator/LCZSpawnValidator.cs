using Godot;
using System;

public partial class LCZSpawnValidator : SpawnValidator
{
    public override bool IsSpawnValid(Layout layout, LayoutCell cell)
    {
        return cell.GridPosition.X < 6;
    }
}
