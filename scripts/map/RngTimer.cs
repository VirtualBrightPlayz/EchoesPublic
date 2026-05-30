using System;
using Godot;

[GlobalClass]
public partial class RngTimer : Timer
{
    [Export]
    public double MinValue = 1f;
    [Export]
    public double MaxValue = 2f;

    public override void _EnterTree()
    {
        Timeout += OnFin;
        OnFin();
    }

    public override void _ExitTree()
    {
        Timeout -= OnFin;
    }

    private void OnFin()
    {
        WaitTime = GD.RandRange(MinValue, MaxValue);
    }
}
