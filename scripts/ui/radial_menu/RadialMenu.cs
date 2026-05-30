using Godot;
using System;
using System.Linq;

public partial class RadialMenu : Container
{
#region Constant
    const float InternalMinWidth = 0.01f;
#endregion

#region Properties
    [Signal]
    public delegate void HoveredEventHandler(Node child);

    [Signal]
    public delegate void SelectedEventHandler(Node child);

    [Export]
    public bool Snap = false;

    [Export(PropertyHint.Range, "0, 100")]
    public float MinDistancePercent;

    private PackedScene _centerNode;
    [Export]
    public PackedScene CenterNode
    {
        get => _centerNode;
        set
        {
            _centerNode = value;
            if (!HasNode("RadialMenu/CenterNode"))
                return;
            var menu = GetNode<Control>("RadialMenu/CenterNode");

            var oldNodes = menu.GetChildren();
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (CanvasItem child in oldNodes)
            {
                child.Visible = false;
                child.QueueFree();
            }

            if (value == null)
                return;

            menu.AddChild(value.Instantiate());
        }
    }

    private float _minWidth = 0.5f;
    [Export(PropertyHint.Range, "0.01, 1.0, 0.01")]
    public float MinWidth
    {
        get => _minWidth;
        set
        {
            if (value + InternalMinWidth > 1)
                return;

            _minWidth = value;
            SetShaderParameter("width_min", value);

            // Handle case where we're now bigger than the minimum size
            if (MaxWidth - value < InternalMinWidth)
                MaxWidth = value + InternalMinWidth * 2;

            var min_width = GetMinSize() * value;
            if (HasNode("RadialMenu/CenterNode"))
                GetNode<Control>("RadialMenu/CenterNode").CustomMinimumSize = new Vector2(min_width, min_width);

            EmitSignal("sort_children");
        }
    }

    private float _maxWidth = 1.0f;
    [Export(PropertyHint.Range, "0.01, 1.0, 0.01")]
    public float MaxWidth
    {
        get => _maxWidth;
        set
        {
            if (value - InternalMinWidth < 0)
                return;
            
            _maxWidth = value;
            SetShaderParameter("width_max", value);

            // Handle case where we're now smaller than the minimum size
            if (value - MinWidth < InternalMinWidth)
                MinWidth = value - InternalMinWidth * 2;

            EmitSignal("sort_children");
        }
    }
    
    private float _cursorSize = 0.4f;
    [Export(PropertyHint.Range, "0, 3.1416, 0.1")]
    public float CursorSize
    {
        get => _cursorSize;
        set
        {
            _cursorSize = value;
            SetShaderParameter("cursor_size", value);
        }
    }

    private float _cursorDegrees = 0.4f;
    [Export(PropertyHint.Range, "-3.1416, 3.1416")]
    public float CursorDegrees
    {
        get => _cursorDegrees;
        set
        {
            _cursorDegrees = value;
            SetShaderParameter("cursor_deg", value);
        }
    }

    private float _cursorTarget = 0.4f;
    [Export]
    public float CursorTarget
    {
        get => _cursorTarget;
        set
        {
            _cursorTarget = value;

            if (Snap && IsInsideTree())
            {
                var indexOffset = CursorPos.IndexOffset;
                CursorDegrees = (float)((double)_cursorTarget).RoundToNearestMultipleOfFactor(indexOffset);
            }
            else
            {
                CursorDegrees = value;
            }
        }
    }

    private Color _backgroundColor = new Color("202431");
    [Export]
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            SetShaderParameter("color_bg", value);
        }
    }

    private Color _foregroundColor = new Color("595f70");
    [Export]
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            _foregroundColor = value;
            SetShaderParameter("color_fg", value);
        }
    }

    private bool _enableHighlighting = true;
    private bool EnableHighlighting
    {
        get => _enableHighlighting;
        set
        {
            _enableHighlighting = value;
            SetShaderParameter("highlight_enabled", value);
        }
    }

    private float _bevelWidth = 0.5f;
    [Export(PropertyHint.Range, "0, 1.0")]
    public float BevelWidth
    {
        get => _bevelWidth;
        set
        {
            _bevelWidth = value;
            SetShaderParameter("bevel_width", value / 5f);
        }
    }

    private bool _bevelEnabled = false;
    [Export]
    public bool BevelEnabled
    {
        get => _bevelEnabled;
        set
        {
            _bevelEnabled = value;
            SetShaderParameter("bevel_enabled", value);
        }
    }

    private Color _bevelColor = new Color("333a4f");
    [Export]
    public Color BevelColor
    {
        get => _bevelColor;
        set
        {
            _bevelColor = value;
            SetShaderParameter("bevel_color", value);
        }
    }

    private bool _modulateEnabled = false;
    [Export]
    public bool ModulateEnabled
    {
        get => _modulateEnabled;
        set
        {
            _modulateEnabled = value;
            DoModulate();
        }
    }

    private Color _modulateHoverColor = Colors.White;
    [Export]
    public Color ModulateHoverColor
    {
        get => _modulateHoverColor;
        set
        {
            _modulateHoverColor = value;
            DoModulate();
        }
    }

    private Color _modulateDefaultColor = new Color("b6b6b6");
    [Export]
    public Color ModulateDefaultColor
    {
        get => _modulateDefaultColor;
        set
        {
            _modulateDefaultColor = value;
            DoModulate();
        }
    }

    public CursorPos CursorPos => GetNode<CursorPos>("RadialMenu/CursorPos");
#endregion

#region Members
    public void Setup()
    {
#pragma warning disable CA2245
        BevelColor = BevelColor;
        BevelEnabled = BevelEnabled;
        BevelWidth = BevelWidth;
        CenterNode = CenterNode;
        BackgroundColor = BackgroundColor;
        ForegroundColor = ForegroundColor;
        CursorDegrees = CursorDegrees;
        CursorTarget = CursorTarget;
        CursorSize = CursorSize;
        ModulateDefaultColor = ModulateDefaultColor;
        ModulateEnabled = ModulateEnabled;
        ModulateHoverColor = ModulateHoverColor;
        MaxWidth = MaxWidth;
        MinWidth = MinWidth;
#pragma warning restore CA2245
    }

    public void SetShaderParameter(string name, Variant newValue)
    {
        if (GetChildren2().Length == 0)
            return;
        ((ShaderMaterial)GetNode<ColorRect>("RadialMenu/Background").Material).SetShaderParameter(name, newValue);
    }

    public float GetMinSize()
    {
        var size = Size;
        return Math.Min(size.X, size.Y);
    }

    public Node[] GetChildren2(bool includeInternal = false) => 
        base.GetChildren(includeInternal)
            .SkipLast(1)    // Remove as many child nodes as we have for the RadialMenu
                        // the remaining array will be all user added children
            .ToArray();

    public void PlaceButtons()
    {
        var buttons = GetChildren2();
        if (buttons.Length == 0)
            return;

        var angleIncrement = (2 * Math.PI) / buttons.Length;
        var rect = GetRect();
        var center = rect.Size / 2;

        var minSize = GetMinSize();
        var minVec = new Vector2(minSize, minSize);

        float angle = 0;
        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
        foreach (Control button in buttons)
        {
            var cornerPos = Vector2.FromAngle(angle);
            cornerPos *= minVec / 2;

            if (minVec.LengthSquared() > 0)
                cornerPos *= Vector2.One - (button.Size / minVec) * 3;

            cornerPos -= button.Size / 2;
            cornerPos += center;

            button.Position = cornerPos;

            // Advance to next angle position
            angle += (float)angleIncrement;

            // Disable focus rectangle
            button.FocusMode = FocusModeEnum.None;
        }

        DoModulate();
    }

    public void AddButton(Node button)
    {
        AddChild(button);
        PlaceButtons();
    }

    private void DoModulate(Node hovered = null)
    {
        var defaultColor = Colors.White;
        
        if (ModulateEnabled)
            defaultColor = ModulateDefaultColor;

        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
        foreach (CanvasItem child in GetChildren())
            child.Modulate = defaultColor;

        if (hovered != null)
            ((CanvasItem)hovered).Modulate = ModulateHoverColor;
    }
#endregion

#region Events
    private void OnSortChildren()
    {
        PlaceButtons();

        var minSize = GetMinSize();

        var radialMenu = GetNode<Control>("RadialMenu");
        radialMenu.AnchorLeft = (float)Anchor.Begin;
        radialMenu.AnchorTop = (float)Anchor.Begin;
        radialMenu.AnchorRight = (float)Anchor.End;
        radialMenu.AnchorBottom = (float)Anchor.End;

        // Resize the background
        var background = GetNode<ColorRect>("RadialMenu/Background");
        background.CustomMinimumSize = new Vector2(minSize, minSize);
        background.PivotOffset = new Vector2(minSize / 2, minSize / 2);

        // Tell our cursor how can be selected
        if (!Engine.IsEditorHint())
            CursorPos.Count = GetChildren2().Length;
    }

    private void OnSelected(int index)
    {
        Node child = GetChildren2().RadialIndexer(index);

        if (child is BaseButton baseButton)
        {
            baseButton.ButtonPressed = true;
            baseButton.EmitSignal(BaseButton.SignalName.Pressed);
        }

        EmitSignal(SignalName.Selected, child);

        if (ModulateEnabled)
            DoModulate();
    }

    private void OnHover(int index)
    {
        EnableHighlighting = index != int.MaxValue;
        Node child = GetChildren2().RadialIndexer(index);

        EmitSignal(SignalName.Hovered, child);

        if (ModulateEnabled)
            DoModulate(child);
    }

    public override void _Input(InputEvent @event)
    {
        var pos = CursorPos.Cursor;
        CursorTarget = (float)Mathf.Atan2(pos.Y, pos.X);
    }

    public override void _EnterTree()
    {
    }

    public override void _Ready()
    {
        AddChild(ResourceLoader.Load<PackedScene>("res://scenes/player/RadialMenu.tscn").Instantiate());
        Setup();
        PlaceButtons();

        var cursorPos = CursorPos;
        cursorPos.Connect(CursorPos.SignalName.Hovered, new Callable(this, nameof(OnHover)));
        cursorPos.Connect(CursorPos.SignalName.Selected, new Callable(this, nameof(OnSelected)));
        cursorPos.MinDistancePercent = MinDistancePercent;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren)
            OnSortChildren();
    }
#endregion
}