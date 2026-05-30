using Godot;

public partial class MicVolumeSlider : HSlider
{
    [Export]
    public Label label;
    [Export]
    public bool rawLabel = true;

    public override void _Ready()
    {
        Value = Settings.User.MicVolumeDb;
        ValueChanged += OnChanged;
        UpdateLabel();
    }

    public override void _ExitTree()
    {
        ValueChanged -= OnChanged;
    }

    public void UpdateLabel()
    {
        if (!IsInstanceValid(label))
            return;
        if (rawLabel)
            label.Text = Value.ToString("0.00");
        else
            label.Text = (Value * 100d).ToString("0") + "%";
    }

    private void OnChanged(double value)
    {
        if (!IsVisibleInTree())
            return;
        Settings.User.MicVolumeDb = (float)value;
        Settings.Modified = true;
        UpdateLabel();
    }
}
