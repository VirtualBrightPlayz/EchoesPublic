using System.Collections.Generic;
using Godot;

public partial class LayoutEditorRoomsList : Node
{
    [Export]
    public LayoutEditor editor;
    [Export]
    public Node3D preview3d;
    [Export]
    public Control container;
    [Export]
    public Button template;
    [Export]
    public OptionButton categoryBtn;
    [Export]
    public LineEdit searchEdit;

    public List<string> categoryList = new List<string>();

    public override void _Ready()
    {
        base._Ready();

        categoryList.Clear();
        foreach (var preset in editor.data.Rooms)
        {
            if (!string.IsNullOrEmpty(preset.category) && !categoryList.Contains(preset.category))
            {
                categoryList.Add(preset.category);
            }
        }

        categoryBtn.Clear();
        categoryBtn.AddItem("All");
        foreach (var cat in categoryList)
        {
            categoryBtn.AddItem(cat);
        }
        categoryBtn.ItemSelected += (_) => ListRooms();
        categoryBtn.Select(0);

        searchEdit.TextSubmitted += (_) => ListRooms();
        searchEdit.FocusExited += ListRooms;

        ListRooms();
    }

    private void AddPreset(PresetRoom room)
    {
        if (!string.IsNullOrWhiteSpace(searchEdit.Text) && !room.ResourceName.Contains(searchEdit.Text, System.StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }
        var btn = template.Duplicate() as Button;
        btn.Visible = true;
        btn.Text = room.ResourceName;
        btn.Icon = editor.roomTextures[(int)room.type];
        btn.Pressed += () =>
        {
            editor.toolAddRoomPreset = room;
            editor.toolButtons[(int)LayoutEditor.ToolType.Add].ButtonPressed = true;
        };
        btn.MouseEntered += () =>
        {
            PreviewRoom(room);
        };
        container.AddChild(btn);
    }

    public void PreviewRoom(PresetRoom room)
    {
        foreach (var ch in preview3d.GetChildren())
        {
            ch.QueueFree();
        }

        if (IsInstanceValid(room.SceneCached))
        {
            var node = room.SceneCached.Instantiate<Node3D>();
            node.ProcessMode = ProcessModeEnum.Disabled;
            preview3d.AddChild(node);
        }
    }

    public void ListRooms()
    {
        foreach (var ch in container.GetChildren())
        {
            if (ch == template)
                continue;

            ch.QueueFree();
        }

        int idx = categoryBtn.Selected - 1;
        if (idx < 0 || idx >= categoryList.Count)
        {
            foreach (var preset in editor.data.Rooms)
            {
                AddPreset(preset);
            }
        }
        else
        {
            string category = categoryList[idx];
            foreach (var preset in editor.data.Rooms)
            {
                if (preset.category == category)
                {
                    AddPreset(preset);
                }
            }
        }
    }
}