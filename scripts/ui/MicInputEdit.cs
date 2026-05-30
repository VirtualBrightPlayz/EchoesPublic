using System;
using Godot;

public partial class MicInputEdit : OptionButton
{
    public override void _Ready()
    {
        VisibilityChanged += Refresh;
        Refresh();
        ItemSelected += OnChanged;
    }

    public override void _ExitTree()
    {
        ItemSelected -= OnChanged;
    }

    private void Refresh()
    {
        if (!IsVisibleInTree())
            return;
        Clear();
        foreach (var item in AudioServer.GetInputDeviceList())
        {
            AddItem(item);
        }
        Selected = Array.IndexOf(AudioServer.GetInputDeviceList(), Settings.User.MicInputName);
    }

    private void OnChanged(long index)
    {
        if (!IsVisibleInTree())
            return;
        Settings.User.MicInputName = AudioServer.GetInputDeviceList()[index];
        Settings.Modified = true;
    }
}