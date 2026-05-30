using Godot;
using System;

public partial class VoiceDebugEdit : CheckBox
{
    [Export]
    public Settings.VoiceDebugFlags flag;

    public override void _Ready()
    {
        ButtonPressed = Settings.User.VoiceDebug.HasFlag(flag);
        Pressed += OnChanged;
    }

    public override void _ExitTree()
    {
        Pressed -= OnChanged;
    }

    private void OnChanged()
    {
        if (!IsVisibleInTree())
            return;
        if (ButtonPressed)
            Settings.User.VoiceDebug = Settings.User.VoiceDebug | flag;
        else
            Settings.User.VoiceDebug = Settings.User.VoiceDebug & ~flag;
        MenuManager.Instance.WriteSettings();
        MenuManager.Instance.UpdateSettings();
    }
}
