/// <summary>
/// Represents a settings slider.
/// </summary>
public interface ISettingsSlider : ISettingsControl
{
    /// <summary>
    /// Represents the minimum value of the slider.
    /// </summary>
    public double MinValue { get; set; }

    /// <summary>
    /// Represents the maximum value of the slider.
    /// </summary>
    public double MaxValue { get; set; }

    /// <summary>
    /// Represents the step of the slider.
    /// </summary>
    public double Step { get; set; }
}
