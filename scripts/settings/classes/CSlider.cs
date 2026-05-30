using Godot;
using System;

[GlobalClass]
[Tool]
public partial class CSlider : BaseSettingsControl, ISettingsSlider
{
	[Export]
	protected HSlider SliderControl { get; set; }

	[Export]
	protected Label MinLabel { get; set; }

	[Export]
	protected Label MaxLabel { get; set; }

	[Export]
	protected SpinBox SpinBox { get; set; }

	public double MinValue { get; set; }

	public double MaxValue { get; set; }

	public double Step { get; set; }

    public override void _EnterTree()
    {
        base._EnterTree();
        SpinBox.ValueChanged += SpinBox_ValueChanged;
        SliderControl.ValueChanged += SliderControl_ValueChanged;
    }

    private void SliderControl_ValueChanged(double value)
    {
        if (value > SliderControl.MaxValue || value < SliderControl.MinValue)
        {
            string msg = $"Slider Value is below {SliderControl.MinValue} or above {SliderControl.MaxValue}";
            ShowMessage(MessageLevel.Error, msg);
            return;
        }
        HideMessage();
        SpinBox.Value = value;
    }

    private void SpinBox_ValueChanged(double value)
    {
        if (value > SliderControl.MaxValue || value < SliderControl.MinValue)
        {
            string msg = $"SpinBox Value is below {SliderControl.MinValue} or above {SliderControl.MaxValue}";
			ShowMessage(MessageLevel.Error, msg);
			return;
        }
		HideMessage();
		SliderControl.Value = value;
    }

    public override void EditorOnElementCreated()
    {
        base.EditorOnElementCreated();
		if (!Mathf.IsZeroApprox(MinValue))
		{
            SliderControl.Set(HSlider.PropertyName.MinValue, MinValue);
			SliderControl.Set(HSlider.PropertyName.Value, (MinValue + MaxValue) / 2);
        }
        else
        {
            Log.PrintWarn($"No minimum specified for {Setting.MemberName}!");
        }
        SliderControl.Set(HSlider.PropertyName.MaxValue, MaxValue);
		if (Step > 0)
		{
			SliderControl.Set(HSlider.PropertyName.Step, Step);
		}
		SpinBox.Set(SpinBox.PropertyName.MinValue, SliderControl.MinValue);
		SpinBox.Set(SpinBox.PropertyName.MaxValue, SliderControl.MaxValue);
		SpinBox.Set(SpinBox.PropertyName.Step, SliderControl.Step);
		SpinBox.Set(SpinBox.PropertyName.Value, SliderControl.Value);
		MinLabel.Set(Label.PropertyName.Text, MinValue.ToString());
		MaxLabel.Set(Label.PropertyName.Text, MaxValue.ToString());
    }

	public override object Value
	{
		get
		{
			return SliderControl.Value;
		}
		set
		{
			double d = (double)Convert.ChangeType(value, typeof(double));
			SliderControl.Value = d;
		}
	}

    public override void Load()
    {
        base.Load();
		SpinBox.Value = SliderControl.Value;
    }

	public override bool Enabled
	{
		get
		{
            if (Engine.IsEditorHint())
            {
                return false;
            }
            if (!IsInstanceValid(SliderControl))
			{
				return false;
			}
			return SliderControl.Editable;
		}
		set
		{
            if (Engine.IsEditorHint())
            {
                return;
            }
            if (!IsInstanceValid(SliderControl))
            {
				return;
            }
			SpinBox.Editable = value;
			SliderControl.Editable = value;
        }
	}

	public override bool Validate(out string value)
	{
		double d = (double)Convert.ChangeType(Value, typeof(double));
		if (d > SliderControl.MaxValue || d < SliderControl.MinValue)
		{
			value = $"Value is below {SliderControl.MinValue} or above {SliderControl.MaxValue}";
			return false;
		}
		if (SpinBox.Value > SliderControl.MaxValue || SpinBox.Value < SliderControl.MinValue)
		{
            value = $"SpinBox Value is below {SliderControl.MinValue} or above {SliderControl.MaxValue}";
			return false;
        }
		value = "";
		return true;
	}
}