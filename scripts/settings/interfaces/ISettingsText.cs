/// <summary>
/// Represents a text entry field in a settings menu
/// </summary>
public interface ISettingsText : ISettingsControl
{
    /// <summary>
    /// Minimum text length (text below this will not pass <see cref="ISettingsControl.Validate"/>).
    /// </summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// Maximum text length (text above this will be ignored by the input field).
    /// </summary>
    public int MinLength { get; set; }
}
