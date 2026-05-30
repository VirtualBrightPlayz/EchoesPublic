using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
[GlobalClass]
public partial class MicrophoneHardwareSelector : BaseSettingHardware<string>
{
    public override HardwareDetectorType DetectorType => HardwareDetectorType.AudioIn;

    public override string Default => "Default";

    public override string DefaultName => Default;

    public override bool AddDefaultToDropdown => false;

    public override void EditorOnElementCreated()
    {
        base.EditorOnElementCreated();
        DropDown.Set(OptionButton.PropertyName.ClipText, true);
        DropDown.Set(OptionButton.PropertyName.FitToLongestItem, false);
    }

    public override int ConvertToIndex(string value)
    {
        for (int i = 0; i < DropDown.ItemCount; i++)
        {
            if (DropDown.GetItemText(i) == value)
            {
                return i;
            }
        }
        return 0;
    }

    public override IEnumerable<string> GetAvailableHardware()
    {
        List<string> devices = AudioServer.GetInputDeviceList().ToList();
        devices.Sort();
        return devices;
    }

    public override IEnumerable<string> GetAvailableHardwareNames()
    {
        return GetAvailableHardware();
    }
}