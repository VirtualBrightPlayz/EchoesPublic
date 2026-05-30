using System;
using Godot;

[Obsolete]
public partial class InputManagerOld : Node
{
    [Export]
    public NodePath inputPath;
    public IPlayerInputSource InputSource => GetNode<IPlayerInputSource>(inputPath);
    public IExtendedPlayerInputSource ExtendedInputSource => GetNode(inputPath) is IExtendedPlayerInputSource src ? src : null;

    public CursorManager Cursor { get; protected set; }

    public Vector2 MouseMotion { get; protected set; } = Vector2.Zero;
    public Vector2 CursorPosition { get; protected set; } = Vector2.Zero;
    public Vector2 MovementDirection { get; protected set; } = Vector2.Zero;
    public bool IsJumpRequested { get; protected set; } = false;
    public bool IsCrouching { get; protected set; } = false;
    public bool IsSprinting { get; protected set; } = false;
    public bool VoiceChat { get; protected set; } = false;
    public bool VoiceChatAlt { get; protected set; } = false;
    public ButtonInputFlags Use { get; protected set; } = ButtonInputFlags.None;
    public ButtonInputFlags Primary { get; protected set; } = ButtonInputFlags.None;
    public ButtonInputFlags Secondary { get; protected set; } = ButtonInputFlags.None;
    public ButtonInputFlags Reload { get; protected set; } = ButtonInputFlags.None;
    public ButtonInputFlags ThrowItem { get; protected set; } = ButtonInputFlags.None;
    public ButtonInputFlags Spray { get; protected set; } = ButtonInputFlags.None;
    public ButtonInputFlags DebugHideUi  { get; protected set; } = ButtonInputFlags.None;
    public MouseButtonMask MouseMask { get; protected set; } = 0;

    public bool InventoryOpen { get; set; } = false;
    public bool PauseMenuOpen { get; set; } = false;
    public bool GameConsoleOpen { get; set; } = false;
    public bool PlayerListOpen { get; set; } = false;
    public bool AdminMenuOpen { get; set; } = false;
    public bool NoClipEnabled { get; set; } = true;
    public bool OtherMenuOpen { get; set; } = false;
    public virtual bool CursorActive => Cursor.IsActive;

    public bool CanOpenInventory { get; set; } = true;
    public bool CanOpenAdminMenu { get; set; } = true;

    public static ButtonInputFlags ResetFlags(ButtonInputFlags flags)
    {
        if (flags.HasFlag(ButtonInputFlags.Pressed))
            return ButtonInputFlags.JustReleased;
        else
            return ButtonInputFlags.None;
    }

    public virtual void OnSpawned(RoleID role)
    {
        NoClipEnabled = true;
    }

    public void ResetInputs(bool full = false)
    {
        MouseMotion = Vector2.Zero;
        MovementDirection = Vector2.Zero;
        IsJumpRequested = false;
        IsCrouching = false;
        IsSprinting = false;
        VoiceChat = false;
        VoiceChatAlt = false;
        if (full)
        {
            CursorPosition = Vector2.Zero;
            Use = ButtonInputFlags.None;
            Primary = ButtonInputFlags.None;
            Secondary = ButtonInputFlags.None;
            Reload = ButtonInputFlags.None;
            ThrowItem = ButtonInputFlags.None;
            Spray = ButtonInputFlags.None;
            DebugHideUi =  ButtonInputFlags.None;
            InventoryOpen = false;
            PauseMenuOpen = false;
            GameConsoleOpen = false;
            PlayerListOpen = false;
            AdminMenuOpen = false;
            OtherMenuOpen = false;
            CanOpenInventory = true;
            CanOpenAdminMenu = true;
        }
        else
        {
            Use = ResetFlags(Use);
            Primary = ResetFlags(Primary);
            Secondary = ResetFlags(Secondary);
            Reload = ResetFlags(Reload);
            ThrowItem = ResetFlags(ThrowItem);
            Spray = ResetFlags(Spray);
            DebugHideUi = ResetFlags(DebugHideUi);
        }
    }

    public override void _Ready()
    {
        Cursor = new CursorManager(this);
        CursorPosition = Vector2.One * GetTree().Root.Size / 2f;
    }

    public override void _Process(double delta)
    {
        if (!Multiplayer.HasMultiplayerPeer() || !IsMultiplayerAuthority())
        {
            ResetInputs(true);
            return;
        }
        ProcessInputs(delta, true);
        // if (InputSource.IsTouch)
        {
            // ProjectSettings.SetSetting("input_devices/pointing/emulate_mouse_from_touch", !Cursor.CursorLocked);
        }
        if (InputSource.IsJoy && !InputSource.IsTouch)
        {
            if (InputSource.PrimaryFire.HasFlag(ButtonInputFlags.JustPressed))
            {
                MouseMask |= MouseButtonMask.Left;
                using var input = new InputEventMouseButton()
                {
                    ButtonIndex = MouseButton.Left,
                    ButtonMask = MouseMask,
                    Pressed = true,
                    Position = CursorPosition,
                    GlobalPosition = CursorPosition,
                    Device = -1,
                };
                GetViewport().PushInput(input);
            }
            if (InputSource.PrimaryFire.HasFlag(ButtonInputFlags.JustReleased))
            {
                MouseMask &= ~MouseButtonMask.Left;
                using var input = new InputEventMouseButton()
                {
                    ButtonIndex = MouseButton.Left,
                    ButtonMask = MouseMask,
                    Pressed = false,
                    Position = CursorPosition,
                    GlobalPosition = CursorPosition,
                    Device = -1,
                };
                GetViewport().PushInput(input);
            }
            if (InputSource.SecondaryFire.HasFlag(ButtonInputFlags.JustPressed))
            {
                MouseMask |= MouseButtonMask.Right;
                using var input = new InputEventMouseButton()
                {
                    ButtonIndex = MouseButton.Right,
                    ButtonMask = MouseMask,
                    Pressed = true,
                    Position = CursorPosition,
                    GlobalPosition = CursorPosition,
                    Device = -1,
                };
                GetViewport().PushInput(input);
            }
            if (InputSource.SecondaryFire.HasFlag(ButtonInputFlags.JustReleased))
            {
                MouseMask &= ~MouseButtonMask.Right;
                using var input = new InputEventMouseButton()
                {
                    ButtonIndex = MouseButton.Right,
                    ButtonMask = MouseMask,
                    Pressed = false,
                    Position = CursorPosition,
                    GlobalPosition = CursorPosition,
                    Device = -1,
                };
                GetViewport().PushInput(input);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        ProcessInputs(delta, false);
    }

    public virtual void ProcessInputs(double delta, bool toggles)
    {
        if (!Multiplayer.HasMultiplayerPeer() || !IsMultiplayerAuthority())
        {
            ResetInputs(true);
            return;
        }
    }
}
