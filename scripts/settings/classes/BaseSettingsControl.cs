using Godot;
using System;
using System.Linq;
using System.Reflection;

[Tool]
public abstract partial class BaseSettingsControl : Container, ISettingsControl
{
    public abstract object Value { get; set; }
    public abstract bool Enabled { get; set; }

    /// <summary>
    /// Setting name label.
    /// </summary>
    [Export]
    public Label Label { get; set; }

    /// <summary>
    /// Message label for <see cref="ShowMessage(MessageLevel, string)"/>.
    /// </summary>
    [Export]
    public Label MessageLabel { get; set; }

    [Export]
    public SettingsResource Setting { get; set; }

    [Export]
    private Godot.Collections.Dictionary<MessageLevel, Color> LevelToColor { get; set; } = new Godot.Collections.Dictionary<MessageLevel, Color>()
    {
        [MessageLevel.Error] = new Color(1f, 0f, 0f),
        [MessageLevel.Warning] = new Color(1f, 0.9176f, 0f),
        [MessageLevel.Information] = new Color(1f, 1f, 1f),
        [MessageLevel.Success] = new Color(0f, 1f, 0f)
    };

    /// <summary>
    /// The currently displayed messages level
    /// </summary>
    public MessageLevel CurrentLevel { get; protected set; }

    /// <summary>
    /// The currently displayed message,
    /// </summary>
    public string Message { get; protected set; }

    public event Action<object> Saved;

    /// <summary>
    /// Invoke the <see cref="Saved"/> event.
    /// </summary>
    /// <param name="obj"></param>
    protected void InvokeSaved(object obj)
    {
        Saved?.Invoke(obj);
    }

    public event Action<object> Loaded;

    /// <summary>
    /// Invoke the <see cref="Loaded"/> event.
    /// </summary>
    /// <param name="obj"></param>
    protected void InvokeLoaded(object obj)
    {
        Loaded?.Invoke(obj);
    }

    /// <summary>
    /// Called when the editor creates this element. Should not contain any engine-runtime code.
    /// </summary>
    public virtual void EditorOnElementCreated()
    {
        if (!IsInstanceValid(Label))
        {
            return;
        }
        if (!Engine.IsEditorHint())
        {
            return;
        }
        Label.Set(Label.PropertyName.Text, Setting.Name);
        Label.Set(Label.PropertyName.TooltipText, Setting.Tooltip);
        Label.Set(Label.PropertyName.MouseFilter, (long)MouseFilterEnum.Stop);
        Label.Set(Label.PropertyName.HorizontalAlignment, (int)HorizontalAlignment.Right);
        Set(Control.PropertyName.TooltipText, Setting.Tooltip);
    }

    /// <summary>
    /// Shows the message coloring the <see cref="MessageLabel"/> and making it visible.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="message"></param>
    public virtual void ShowMessage(MessageLevel level, string message)
    {
        if (IsInstanceValid(MessageLabel))
        {
            MessageLabel.Text = message;
            //avoid having to add another thing to the dict.
            if (level == MessageLevel.None)
            {
                level = MessageLevel.Information;
            }
            MessageLabel.AddThemeColorOverride("font_color", LevelToColor[level]);
            MessageLabel.Visible = true;
            Message = message;
            CurrentLevel = level;
        }
    }

    /// <summary>
    /// Hides the <see cref="MessageLabel"/> and unsets its text and color.
    /// </summary>
    public virtual void HideMessage()
    {
        if (IsInstanceValid(MessageLabel))
        {
            MessageLabel.Visible = false;
            MessageLabel.Text = "";
            MessageLabel.RemoveThemeColorOverride("font_color");
            Message = "";
            CurrentLevel = MessageLevel.None;
        }
    }

    public virtual void OnValidateFailed(string reason)
    {
        ShowMessage(MessageLevel.Error, reason);
    }

    public virtual void Load()
    {
        if (Setting == null)
        {
            Log.PrintWarn($"Setting is null here! {GetType().FullName}");
        }
        if (Setting?.Info.MemberType == MemberTypes.Field)
        {
            var info = Setting.Info as FieldInfo;
            Value = info.GetValue(Setting.SettingsObject);
        }
        else if (Setting.Info.MemberType == MemberTypes.Property)
        {
            var info = Setting.Info as PropertyInfo;
            Value = info.GetValue(Setting.SettingsObject);
        }
        //TooltipText = Setting?.Tooltip;
        //Label.Text = Setting.Name;
        Loaded?.Invoke(Value);
    }

    public virtual void Reset()
    {
        if (Setting == null)
        {
            Log.PrintWarn($"Setting is null here! {GetType().FullName}");
        }
        if (Setting?.Info.MemberType == MemberTypes.Field)
        {
            Value = (Setting.Info as FieldInfo).GetValue(Activator.CreateInstance(Setting.SettingsObject.GetType()));
        }
        else if (Setting?.Info.MemberType == MemberTypes.Property)
        {
            Value = (Setting.Info as PropertyInfo).GetValue(Activator.CreateInstance(Setting.SettingsObject.GetType()));
        }
        Save();
        Load();
    }

    public virtual void Save()
    {
        if (Setting == null)
        {
            Log.PrintWarn($"Setting is null here! {GetType().FullName}");
        }
        bool changed = false;
        if (Setting?.Info.MemberType == MemberTypes.Field)
        {
            var info = Setting.Info as FieldInfo;
            object val;
            if (info.FieldType.IsSubclassOf(typeof(Enum)))
            {
                val = Convert.ChangeType(Value, Enum.GetUnderlyingType(info.FieldType));
            }
            else
            {
                val = Convert.ChangeType(Value, info.FieldType);
            }
            if (!info.GetValue(Setting.SettingsObject).Equals(val))
            {
                changed = true;
            }
            info.SetValue(Setting.SettingsObject, val);
        }
        else if (Setting?.Info.MemberType == MemberTypes.Property)
        {
            var info = Setting.Info as PropertyInfo;
            object val;
            if (info.PropertyType.IsSubclassOf(typeof(Enum)))
            {
                val = Convert.ChangeType(Value, Enum.GetUnderlyingType(info.PropertyType));
            }
            else
            {
                val = Convert.ChangeType(Value, info.PropertyType);
            }
            if (!info.GetValue(Setting.SettingsObject).Equals(val))
            {
                changed = true;
            }
            info.SetValue(Setting.SettingsObject, val);
        }
        if (changed && Setting?.Info?.GetCustomAttribute<RestartRequiredAttribute>() != null)
        {
            ShowMessage(MessageLevel.Information, "Restart is required to apply this setting.");
        }
        Saved?.Invoke(Value);
    }

    public virtual void OnPreValidate()
    {
        if (!Enabled)
        {
            return;
        }
        HideMessage();
    }

    public abstract bool Validate(out string reason);

    public override int[] _GetAllowedSizeFlagsHorizontal()
    {
        return [(int)SizeFlags.Fill, (int)SizeFlags.ShrinkBegin, (int)SizeFlags.ShrinkCenter, (int)SizeFlags.ShrinkEnd];
    }

    public override int[] _GetAllowedSizeFlagsVertical()
    {
        return [(int)SizeFlags.Fill, (int)SizeFlags.ShrinkBegin, (int)SizeFlags.ShrinkCenter, (int)SizeFlags.ShrinkEnd];
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationSortChildren)
        {
            foreach (var c in GetChildren().OfType<Control>())
            {
                FitChildInRect(c, new Rect2(Vector2.Zero, Size));
            }
        }
    }

    /// <summary>
    /// Message level for <see cref="ShowMessage(MessageLevel, string)"/>.
    /// </summary>
    public enum MessageLevel
    {
        None = -1,
        Success,
        Information,
        Warning,
        Error
    }
}