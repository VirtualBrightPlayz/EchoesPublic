using System;

/// <summary>
/// Represents a settings button. Not yet implemented.
/// </summary>
public interface ISettingsButton : ISettingsControl
{
    /// <summary>
    /// The action that will be called when the button is clicked.
    /// </summary>
    public Action Clicked { get; set; }
}