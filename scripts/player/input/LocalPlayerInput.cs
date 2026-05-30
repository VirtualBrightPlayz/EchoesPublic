using System;
using Godot;

public partial class LocalPlayerInput : Node, IPlayerInputSource, IExtendedPlayerInputSource
{
    public static StringName PlayerJump = "Jump";
    public static StringName PlayerSneak = "Crouch";
    public static StringName PlayerSprint = "Sprint";
    public static StringName PlayerUse = "Use";
    public static StringName PlayerPrimary = "Primary";
    public static StringName PlayerSecondary = "Secondary";
    public static StringName PlayerReload = "Reload";
    public static StringName PlayerThrow = "Throw";
    public static StringName PlayerVoiceChat = "ProximityVoice";
    public static StringName PlayerVoiceChatAlt = "TeamVoice";
    public static StringName PlayerInventory = "Inventory";
    public static StringName MenuPause = "Pause";
    public static StringName PlayerListName = "PlayerList";
    public static StringName AdminMenuName = "AdminMenu";
    public static StringName AdminNoclip = "NoClip";
    public static StringName PlayerSpray = "player_spray";

    public static StringName PlayerMove = "Move";
    public static StringName PlayerCamera = "Camera";

    public static StringName CameraLockMenu = "CameraLockMenu";
    public static StringName ClickLockMenu = "ClickLockMenu";
    public static StringName ExitLockMenu = "ExitLockMenu";

    public static StringName player_left = "player_left";
    public static StringName player_right = "player_right";
    public static StringName player_forward = "player_forward";
    public static StringName player_backward = "player_backward";

    public static StringName player_joystick_left = "player_joystick_left";
    public static StringName player_joystick_right = "player_joystick_right";
    public static StringName player_joystick_forward = "player_joystick_forward";
    public static StringName player_joystick_backward = "player_joystick_backward";

    public static StringName player_camera_joystick_right = "player_camera_joystick_right";
    public static StringName player_camera_joystick_left = "player_camera_joystick_left";
    public static StringName player_camera_joystick_down = "player_camera_joystick_down";
    public static StringName player_camera_joystick_up = "player_camera_joystick_up";
    
    public static StringName debug_player_hide_ui = "HideUI";
    public static StringName InventoryOne = "InventoryOne";
    public static StringName InventoryTwo = "InventoryTwo";

    public bool CanInput => Multiplayer.HasMultiplayerPeer() && Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected ? IsMultiplayerAuthority() : true;
    public bool IsMouse { get; set; }
    public bool IsJoy { get; set; }
    public bool IsTouch { get; set; }
    public bool IsVR => false;
    public Vector2 MouseMotion { get; set; } = Vector2.Zero;
    public Vector2 MovementDirection => IsMouse ? MovementDirectionKeyboard : LeftJoystick;
    public ButtonInputFlags Jump => IPlayerInputSource.GetInput(PlayerJump);
    public ButtonInputFlags Crouch => IPlayerInputSource.GetInput(PlayerSneak);
    public ButtonInputFlags Sprint => IPlayerInputSource.GetInput(PlayerSprint);
    public ButtonInputFlags Use => IPlayerInputSource.GetInput(PlayerUse);
    public ButtonInputFlags PrimaryFire => IPlayerInputSource.GetInput(PlayerPrimary);
    public ButtonInputFlags SecondaryFire => IPlayerInputSource.GetInput(PlayerSecondary);
    public ButtonInputFlags Reload => IPlayerInputSource.GetInput(PlayerReload);
    public ButtonInputFlags ThrowItem => IPlayerInputSource.GetInput(PlayerThrow);
    public ButtonInputFlags VoiceChat => IPlayerInputSource.GetInput(PlayerVoiceChat);
    public ButtonInputFlags VoiceChatAlt => IPlayerInputSource.GetInput(PlayerVoiceChatAlt);
    public ButtonInputFlags Inventory => IPlayerInputSource.GetInput(PlayerInventory);
    public ButtonInputFlags PauseMenu => IPlayerInputSource.GetInput(MenuPause);

    public ButtonInputFlags DebugHideUi => IPlayerInputSource.GetInput(debug_player_hide_ui);

    // public ButtonInputFlags GameConsole => IPlayerInputSource.GetInput("menu_console");
    public ButtonInputFlags PlayerList => IPlayerInputSource.GetInput(PlayerListName);
    public ButtonInputFlags AdminMenu => IPlayerInputSource.GetInput(AdminMenuName);
    public ButtonInputFlags NoClip => IPlayerInputSource.GetInput(AdminNoclip);
    public ButtonInputFlags Spray => IPlayerInputSource.GetInput(PlayerSpray);

    public Vector2 MovementDirectionKeyboard => Input.GetVector(player_left, player_right, player_forward, player_backward);
    public Vector2 LeftJoystick => IInitScript.IsTestClient ? new Vector2(0f, -1f) : Input.GetVector(player_joystick_left, player_joystick_right, player_joystick_forward, player_joystick_backward);
    public Vector2 RightJoystick => Input.GetVector(player_camera_joystick_right, player_camera_joystick_left, player_camera_joystick_down, player_camera_joystick_up);

    public double time = 0d;

    public void Tick(double delta)
    {
        if (IInitScript.IsTestClient)
        {
            time += delta;
            MouseMotion += Vector2.Right * (float)delta;
        }
        if (IsJoy)
            MouseMotion += RightJoystick * 4f * (float)delta * Settings.User.MouseSensitivity;
        if (!OS.GetName().Equals("Android"))
        {
            if (!RightJoystick.IsZeroApprox() || !LeftJoystick.IsZeroApprox())
            {
                // IsMouse = false;
                // IsJoy = true;
                // IsTouch = false;
            }
        }
    }

    public void Flush()
    {
        MouseMotion = Vector2.Zero;
    }

    public override void _Input(InputEvent ev)
    {
        if (!CanInput)
            return;
        if (ev is InputEventJoypadButton || ev is InputEventJoypadMotion)
        {
            if (!OS.GetName().Equals("Android"))
            {
                IsMouse = false;
                IsJoy = true;
                // IsTouch = false;
            }
        }
        if (ev is InputEventKey)
        {
            IsMouse = true;
            IsJoy = false;
            IsTouch = false;
        }
        if (ev is InputEventScreenDrag || ev is InputEventScreenTouch)
        {
            IsMouse = false;
            // IsJoy = false;
            IsTouch = true;
        }
        if (ev is InputEventMouseMotion motion && motion.Device != -1)
        {
            // if (!OS.GetName().Equals("Android"))
            {
                IsMouse = true;
                IsJoy = false;
                IsTouch = false;
                MouseMotion = motion.ScreenRelative * -0.001f * Settings.User.MouseSensitivity;
                // GetViewport().SetInputAsHandled();
            }
        }
    }
}
