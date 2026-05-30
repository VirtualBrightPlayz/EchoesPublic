using System;

/// <summary>
/// Represents a settings control.
/// </summary>
public interface ISettingsControl
{
    /// <summary>
    /// Current Value of the configuration UI element.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// If the element is currently enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Called when a save is done.
    /// </summary>
    public event Action<object> Saved;
    
    /// <summary>
    /// Called when a load is done.
    /// </summary>

    public event Action<object> Loaded;

    /// <summary>
    /// Loads the value and sets the UI controls state
    /// </summary>
    public void Load();

    /// <summary>
    /// Saves the current value stored within the <see cref="Value"/> property.
    /// </summary>
    public void Save();

    /// <summary>
    /// Validates that the value within <see cref="Value"/> is allowed to be stored.
    /// </summary>
    /// <returns></returns>
    public bool Validate(out string reason);

    /// <summary>
    /// Resets the the value and saves it.
    /// </summary>
    public void Reset();

    /// <summary>
    /// Called when validation fails.
    /// </summary>
    public void OnValidateFailed(string reason);

    /// <summary>
    /// Called before the validation call.
    /// </summary>
    public void OnPreValidate();

    /// <summary>
    /// Contains information on the setting value which this control represents.
    /// </summary>
    public SettingsResource Setting { get; set; }
}