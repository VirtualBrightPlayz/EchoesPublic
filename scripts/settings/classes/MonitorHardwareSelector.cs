using Godot;
using System;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class MonitorHardwareSelector : BaseSettingHardware<int>
{
    public override HardwareDetectorType DetectorType => HardwareDetectorType.Screen;

    public override int Default => -1;

    public override string DefaultName => "Default Monitor";

    // public override bool AddDefaultToDropdown => false;

    public override int ConvertToIndex(int value)
    {
        return Math.Max(0, value);
    }

    public override IEnumerable<int> GetAvailableHardware()
    {
        int[] result = new int[DisplayServer.GetScreenCount()];
        for (int i = 0; i < DisplayServer.GetScreenCount(); i++)
        {
            result[i] = i;
        }
        return result;
    }

    public override IEnumerable<string> GetAvailableHardwareNames()
    {
        List<string> result = new List<string>();
        foreach (int i in GetAvailableHardware())
        {
            result.Add($"Monitor {i}");
        }
        return result;
    }
}
