using System;
using Godot;

[Obsolete]
public interface IPlayerInputSource
{
    bool CanInput { get; }
    bool IsMouse { get; }
    bool IsJoy { get; }
    bool IsTouch { get; }
    bool IsVR { get; }
    Vector2 MouseMotion { get; }
    Vector2 MovementDirection { get; }
    ButtonInputFlags Jump { get; }
    ButtonInputFlags Crouch { get; }
    ButtonInputFlags Sprint { get; }
    ButtonInputFlags Use { get; }
    ButtonInputFlags PrimaryFire { get; }
    ButtonInputFlags SecondaryFire { get; }
    ButtonInputFlags Reload { get; }
    ButtonInputFlags ThrowItem { get; }
    ButtonInputFlags VoiceChat { get; }
    ButtonInputFlags VoiceChatAlt { get; }
    ButtonInputFlags Inventory { get; }
    ButtonInputFlags PauseMenu { get; }
    ButtonInputFlags DebugHideUi { get; }

    void Tick(double delta);
    void Flush();

    public static ButtonInputFlags GetInput(StringName name)
    {
        // if (IInitScript.IsTestClient)
            // return ButtonInputFlags.JustPressed | ButtonInputFlags.Pressed;
        ButtonInputFlags flags = ButtonInputFlags.None;
        if (Input.IsActionPressed(name))
            flags |= ButtonInputFlags.Pressed;
        if (Input.IsActionJustPressed(name))
            flags |= ButtonInputFlags.JustPressed;
        if (Input.IsActionJustReleased(name))
            flags |= ButtonInputFlags.JustReleased;
        return flags;
    }

    public static ButtonInputFlags UpdateInput(bool state, ButtonInputFlags flags)
    {
        if (state != flags.HasFlag(ButtonInputFlags.Pressed))
        {
            if (state)
                flags = ButtonInputFlags.JustPressed | ButtonInputFlags.Pressed;
            else
                flags = ButtonInputFlags.JustReleased;
        }
        else if (state)
            flags = ButtonInputFlags.Pressed;
        else
            flags = ButtonInputFlags.None;
        return flags;
    }
}

[Obsolete]
public interface IExtendedPlayerInputSource
{
    // ButtonInputFlags GameConsole { get; }
    ButtonInputFlags PlayerList { get; }
    ButtonInputFlags AdminMenu { get; }
    ButtonInputFlags NoClip { get; }
    ButtonInputFlags Spray { get; }
}

[Flags]
public enum ButtonInputFlags : byte
{
    None = 0,
    JustPressed = 1,
    JustReleased = 2,
    Pressed = 4,
    Max = 8,
}
