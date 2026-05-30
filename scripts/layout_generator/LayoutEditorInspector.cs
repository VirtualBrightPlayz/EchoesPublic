using System.Collections.Generic;
using Godot;

public partial class LayoutEditorInspector : Node
{
    [Export]
    public LayoutEditor editor;
    [Export]
    public Control boxContainer;

    public void Inspect(GodotObject node)
    {
        foreach (var item in boxContainer.GetChildren())
        {
            item.QueueFree();
        }
        if (node is LayoutEditorObject2D sp)
        {
            {
                Label label = new Label();
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                label.Text = sp.LayerPosition.ToString() + ' ' + sp.RoomName;
                label.LabelSettings = new LabelSettings()
                {
                    FontSize = 24,
                };
                boxContainer.AddChild(label);
                HSeparator sep = new HSeparator();
                boxContainer.AddChild(sep);
            }
            var propTypes = sp.GetProps();
            foreach (var kvp in propTypes)
            {
                HBoxContainer box = new HBoxContainer();
                box.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                Label label = new Label();
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                label.Text = kvp.Key;
                box.AddChild(label);
                Control edit = AddInspector(sp, kvp.Key, kvp.Value.Item2, kvp.Value.Item1);
                edit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                box.AddChild(edit);
                boxContainer.AddChild(box);
            }
            foreach (var meta in sp.layoutRoom.keyvalues)
            {
                if (propTypes.ContainsKey(meta.Key))
                    continue;
                HBoxContainer box = new HBoxContainer();
                box.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                Label label = new Label();
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                label.Text = meta.Key;
                box.AddChild(label);
                LineEdit edit = new LineEdit();
                edit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                edit.Text = meta.Value?.ToString();
                edit.TextChanged += (_) => UpdateData(sp, meta.Key, edit);
                box.AddChild(edit);
                Button btn = new Button();
                btn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
                btn.Text = "X";
                btn.Pressed += () => RemoveData(sp, meta.Key);
                box.AddChild(btn);
                boxContainer.AddChild(box);
            }
            {
                HSeparator sep = new HSeparator();
                boxContainer.AddChild(sep);
                HBoxContainer box = new HBoxContainer();
                LineEdit edit = new LineEdit();
                edit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                edit.PlaceholderText = "Key";
                box.AddChild(edit);
                Button btn = new Button();
                btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                btn.Text = "Add Metadata";
                btn.Pressed += () => AddData(sp, edit);
                box.AddChild(btn);
                boxContainer.AddChild(box);
            }
        }
    }

    public Control AddInspector(LayoutEditorObject2D node, string key, object value, RoomPropertyData.PropType type)
    {
        switch (type)
        {
            default:
            case RoomPropertyData.PropType.String:
            {
                LineEdit edit = new LineEdit();
                edit.Text = value?.ToString();
                edit.TextChanged += (v) => node.layoutRoom.keyvalues[key] = v;
                return edit;
            }
            case RoomPropertyData.PropType.Int when value is int:
            {
                SpinBox edit = new SpinBox();
                edit.Value = (int)value;
                edit.Rounded = true;
                edit.AllowGreater = true;
                edit.AllowLesser = true;
                edit.ValueChanged += (v) => node.layoutRoom.keyvalues[key] = (int)v;
                return edit;
            }
            case RoomPropertyData.PropType.Float when value is float:
            {
                SpinBox edit = new SpinBox();
                edit.Value = (float)value;
                edit.Rounded = false;
                edit.AllowGreater = true;
                edit.AllowLesser = true;
                edit.ValueChanged += (v) => node.layoutRoom.keyvalues[key] = (float)v;
                return edit;
            }
            case RoomPropertyData.PropType.Bool when value is bool:
            {
                CheckBox edit = new CheckBox();
                edit.ButtonPressed = (bool)value;
                edit.Toggled += (v) => node.layoutRoom.keyvalues[key] = v;
                return edit;
            }
        }
    }

    public void RemoveData(LayoutEditorObject2D node, string meta)
    {
        if (IsInstanceValid(node))
        {
            // node.RemoveMeta(meta);
            node.layoutRoom.keyvalues.Remove(meta);
            Inspect(node);
        }
    }

    public void UpdateData(LayoutEditorObject2D node, string key, LineEdit edit)
    {
        if (IsInstanceValid(node) && IsInstanceValid(edit) && !string.IsNullOrWhiteSpace(edit.Text))
        {
            // node.SetMeta("room_meta_" + edit.Text, string.Empty);
            node.layoutRoom.keyvalues[key] = edit.Text;
            // Inspect(node);
        }
    }

    public void AddData(LayoutEditorObject2D node, LineEdit edit)
    {
        if (IsInstanceValid(node) && IsInstanceValid(edit) && !string.IsNullOrWhiteSpace(edit.Text))
        {
            node.layoutRoom.keyvalues.Add(edit.Text, string.Empty);
            Inspect(node);
        }
    }
}