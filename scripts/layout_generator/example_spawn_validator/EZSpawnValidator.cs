using Godot;
using System;

public partial class EZSpawnValidator : SpawnValidator
{
    [Export]
    public int CheckpointX = 2;
    [Export]
    public bool isEz = true;
    [Export]
    public bool isCheckpoint = false;

    public override bool IsSpawnValid(Layout layout, LayoutCell cell)
    {
        return isCheckpoint ? cell.GridPosition.X == CheckpointX : isEz ? cell.GridPosition.X < CheckpointX : cell.GridPosition.X > CheckpointX;
    }
}