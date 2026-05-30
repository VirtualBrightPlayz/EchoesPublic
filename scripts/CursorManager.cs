using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// TODO: Make this also handle keyboard input (like preventing you from jumping when you're typing in the console).

/// <summary>
/// Manages the cursor's visibility, allowing various menus that
/// require the cursor to be visible to coexist.
/// </summary>
public partial class CursorManager
{
    private CursorType _state;
    private Viewport _viewport;
    private bool _isActive = true;
    private bool _wasLocked = false;
    private static List<CursorManager> _managers = new List<CursorManager>();

    public bool CursorLocked => _state == CursorType.None;
    public bool IsAtComputer => _state.HasFlag(CursorType.Computer);
    public bool ConsoleOpen => _state.HasFlag(CursorType.Console);
    public bool MouseInputAllowed => IsActive && (InteractInputAllowed || _state == CursorType.Spectator);
    public bool MovementInputAllowed => IsActive && (CursorLocked || _state == CursorType.Inventory);
    public bool InteractInputAllowed => IsActive && CursorLocked && !GuiActive;
    public bool GuiEditActive => GodotObject.IsInstanceValid(_viewport?.GuiGetFocusOwner()) && _viewport.GuiGetFocusOwner() is LineEdit edit && edit.IsEditing();
    public bool GuiActive => GodotObject.IsInstanceValid(_viewport?.GuiGetFocusOwner());

    public bool IsActive
    {
        get => (!GodotObject.IsInstanceValid(_viewport) && _isActive) || ((_viewport is Window window && window.HasFocus()) || _viewport.GetWindow().HasFocus() || IInitScript.Instance.IsXR);
        set
        {
            _isActive = value;
            // Update();
        }
    }

    public CursorManager()
    {
        _viewport = null;
        _managers.Add(this);
    }

    public CursorManager(Viewport viewport)
    {
        _viewport = viewport;
        _managers.Add(this);
    }

    public CursorManager(Node node)
    {
        _viewport = node.GetViewport();
        _managers.Add(this);
    }

    ~CursorManager()
    {
        _managers.Remove(this);
    }

    public static CursorManager Get(Node node)
    {
        return _managers.FirstOrDefault(x => x._viewport == node.GetViewport());
    }

    public bool Get(CursorType type)
    {
        return _state.HasFlag(type);
    }

    public void Toggle(CursorType type)
    {
        Set(type, !Get(type));
    }

    /// <summary>
    /// Toggles the specified cursor type, modifying
    /// the cursor on screen as necessary.
    /// </summary>
    public void Set(CursorType type, bool toggled)
    {
        // Change the specified flag.
        if (toggled)
        {
            _state |= type;
        }
        else
        {
            _state &= ~type;
        }

        // Update the cursor to match the current state.
        Update();
    }

    public void SetBulk(CursorType type, bool toggled)
    {
        // Change the specified flag.
        if (toggled)
        {
            _state |= type;
        }
        else
        {
            _state &= ~type;
        }
    }

    /// <summary>
    /// Updates the cursor if needed.
    /// </summary>
    public void Update()
    {
        if (IsActive)
        {
            bool locked = CursorLocked || IsAtComputer;
            var mode = locked ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
            if (locked != _wasLocked && !IInitScript.Instance.IsXR)
            {
                // Input.MouseMode = mode;
            }
            _wasLocked = locked;
            if (locked)
            {
                _viewport?.GuiReleaseFocus();
            }
        }
    }

    /// <summary>
    /// The type of cursor, or the menu using the cursor.
    /// </summary>
    [Flags]
    public enum CursorType : short
    {
        None = 0,
        Pause = 1,
        Console = 2,
        Admin = 4,
        Spectator = 8,
        FreeCam = 16,
        Inventory = 32,
        Computer = 64,
    }
}