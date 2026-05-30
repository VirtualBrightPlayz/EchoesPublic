using DeBroglie;
using DeBroglie.Constraints;
using Godot;
using System;

public partial class SpawnValidator : Resource
{
    public virtual bool IsSpawnValid(Layout layout, LayoutCell cell)
    {
        return false;
    }
}
