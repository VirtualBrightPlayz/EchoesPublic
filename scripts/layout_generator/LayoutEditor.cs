using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class LayoutEditor : Control
{
    public enum ToolType : int
    {
        Select = 0,
        Add = 1,
        Rotate = 2,
        Remove = 3,
    }

    [Signal]
    public delegate void OnTryExitEventHandler();
    [Signal]
    public delegate void OnMapEditedEventHandler();
    [Signal]
    public delegate void OpenFileEventHandler();
    [Signal]
    public delegate void SaveFileEventHandler();
    [Signal]
    public delegate void OpenHelpEventHandler();
    [Signal]
    public delegate void InspectRoomEventHandler(GodotObject node);

    [Export]
    public Camera2D camera;
    [Export]
    public Camera3D previewCamera;
    [Export]
    public Node2D mapRoot;
    [Export]
    public Tree roomList;
    [Export]
    public Tree layerTree;
    public PresetRoom[] rooms => data.Rooms;
    [Export]
    public GameData data;
    [Export]
    public Texture2D[] roomTextures;
    [Export]
    public Vector2 pixelSize = Vector2.One * 16;
    [Export]
    public float roomSize = 20.8f;
    [Export]
    public Godot.Collections.Dictionary<Vector2I, LayoutEditorObject2D> sprites = new Godot.Collections.Dictionary<Vector2I, LayoutEditorObject2D>();
    [Export]
    public Label helpLabel;
    [Export]
    public PopupMenu contextMenu;
    [Export]
    public LayoutEditorObject2D layoutObjectPreset;
    [Export]
    public ColorRect grid;

    public Vector2 contextPosition;

    public MapLayout layout;
    public int activeLayer = 0;
    public ulong saveVersion;
    public Callable destroyAction;

    public List<KeyValuePair<Action, Action>> undoRedo = new List<KeyValuePair<Action, Action>>();
    public int undoRedoIndex = 0;
    public List<LayoutEditorObject2D> spriteCache = new List<LayoutEditorObject2D>();

    public ToolType tool = ToolType.Select;
    public PresetRoom toolAddRoomPreset = null;
    [Export]
    public Godot.Collections.Array<BaseButton> toolButtons = new Godot.Collections.Array<BaseButton>();

    public bool isTreeLoading = false;

    public override void _Ready()
    {
        layout = new MapLayout();
        layout.layers.Add(new MapLayoutLayer());
        saveVersion = 0;
        mapRoot.ChildEnteredTree += _SpriteAdded;
        mapRoot.ChildExitingTree += _SpriteRemoved;

        contextMenu.IndexPressed += _ContextSelected;
        
        layerTree.Clear();
        layerTree.SetColumnExpandRatio(0, 90);
        layerTree.SetColumnExpandRatio(1, 10);
        layerTree.ItemMouseSelected += _TreeSelected;
        TreeItem root = layerTree.CreateItem();
        root.SetText(0, "Layers");
        root.SetText(1, "+");
        root.SetTooltipText(1, "Add Layer");

        if (IsInstanceValid(roomList))
        {
            roomList.Clear();
            roomList.ItemMouseSelected += _RoomListSelected;
            root = roomList.CreateItem();
            Dictionary<string, TreeItem> roomCategoryLookup = new Dictionary<string, TreeItem>();
            for (int i = 0; i < rooms.Length; i++)
            {
                if (!roomCategoryLookup.TryGetValue(rooms[i].category, out TreeItem category))
                {
                    category = root.CreateChild();
                    category.SetText(0, rooms[i].category);
                    roomCategoryLookup.Add(rooms[i].category, category);
                }
                TreeItem item = category.CreateChild();
                item.SetMetadata(0, rooms[i]);
                item.SetIcon(0, roomTextures[(int)rooms[i].type]);
                item.SetText(0, rooms[i].ResourceName);
                // int id = root.AddItem(rooms[i].ResourceName, roomTextures[(int)rooms[i].type]);
            }
            // list.Select(0);
        }

        for (int i = 0; i < toolButtons.Count; i++)
        {
            int j = i;
            toolButtons[i].Toggled += (s) =>
            {
                if (s)
                {
                    SetTool((ToolType)j);
                }
            };
        }

        UpdateLayersTree();
        activeLayer = -1;
        UpdateActiveLayer(0);
        // UpdateLayers();
    }

    public override void _ExitTree()
    {
        ClearHistory();
    }

    private void SetTool(ToolType type)
    {
        tool = type;
    }

    private void UseActiveTool(Vector2I iPos)
    {
        switch (tool)
        {
            default:
            case ToolType.Select:
            {
                if (sprites.TryGetValue(iPos, out var sp))
                {
                    EmitSignalInspectRoom(sp);
                }
                else
                {
                    EmitSignalInspectRoom(null);
                }
                break;
            }
            case ToolType.Add:
            {
                if (sprites.TryGetValue(iPos, out var sprite))
                {
                    AddAction(() => sprite.layoutRoom.AddRotation(), () => sprite.layoutRoom.SubRotation());
                    saveVersion++;
                    // EmitSignalOnMapEdited();
                    // UpdateLayers();
                    // CallDeferred(MethodName.UpdateLayers);
                }
                else if (IsInstanceValid(toolAddRoomPreset))
                {
                    PresetRoom preset = toolAddRoomPreset;
                    MapLayoutRoom room = new MapLayoutRoom()
                    {
                        x = iPos.X,
                        z = iPos.Y,
                        rotation = 0,
                        name = preset.ResourceName,
                        type = (int)preset.type,
                    };
                    MapLayoutLayer layer = layout.layers[activeLayer];
                    AddAction(() => layer.layout.Add(room), () => layer.layout.Remove(room));
                    saveVersion++;
                    // EmitSignalOnMapEdited();
                    // UpdateLayers();
                    // CallDeferred(MethodName.UpdateLayers);
                }
                break;
            }
            case ToolType.Rotate:
            {
                if (sprites.TryGetValue(iPos, out var sprite))
                {
                    AddAction(() => sprite.layoutRoom.AddRotation(), () => sprite.layoutRoom.SubRotation());
                    saveVersion++;
                    // EmitSignalOnMapEdited();
                    // UpdateLayers();
                    // CallDeferred(MethodName.UpdateLayers);
                }
                break;
            }
            case ToolType.Remove:
            {
                if (sprites.TryGetValue(iPos, out var sprite))
                {
                    var layer = layout.layers[activeLayer];
                    AddAction(() => layer.layout.Remove(sprite.layoutRoom), () => layer.layout.Add(sprite.layoutRoom));
                    saveVersion++;
                    // EmitSignalOnMapEdited();
                    // UpdateLayers();
                    // CallDeferred(MethodName.UpdateLayers);
                }
                break;
            }
        }
    }

    public void UpdateLayersTree()
    {
        isTreeLoading = true;
        if (IsInstanceValid(layerTree.GetRoot()))
        {
            var arr = layerTree.GetRoot().GetChildren().ToArray();
            for (int i = 0; i < layout.layers.Count; i++)
            {
                TreeItem item = layerTree.CreateItem(layerTree.GetRoot());
                item.SetMetadata(0, i);
                item.SetText(0, $"Layer {i}");
                item.SetText(1, "X");
                item.SetTooltipText(1, "Delete Layer");
                if (i == activeLayer)
                    layerTree.SetSelected(item, 0);
            }
            foreach (var item in arr)
            {
                item.Free();
            }
        }
        isTreeLoading = false;
        CallDeferred(MethodName.UpdateLayers);
    }

    public void UpdateLayers()
    {
        // await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        List<Vector2I> used = new List<Vector2I>();
        foreach (var kvp in sprites)
        {
            // mapRoot.RemoveChild(kvp.Value);
        }
        // for (int i = 0; i < layout.layers.Count; i++)
        {
            // if (i != activeLayer)
                // continue;
            int i = activeLayer;
            if (i != -1)
            {
                for (int j = 0; j < layout.layers[i].layout.Count; j++)
                {
                    MapLayoutRoom room = layout.layers[i].layout[j];
                    Vector2I iPos = new Vector2I(room.x, room.z);
                    if (sprites.TryGetValue(iPos, out var sp))
                    {
                        sp.Setup(this, room);
                    }
                    else
                    {
                        sp = (LayoutEditorObject2D)layoutObjectPreset.Duplicate();
                        spriteCache.Add(sp);
                        sp.Setup(this, room);
                        mapRoot.AddChild(sp);
                        sprites.Add(iPos, sp);
                    }
                    used.Add(iPos);
                }
            }
        }
        List<Vector2I> rem = new List<Vector2I>();
        foreach (var kvp in sprites)
        {
            if (used.Contains(kvp.Key))
            {
                continue;
            }
            mapRoot.RemoveChild(kvp.Value);
            kvp.Value.QueueFree();
            // sprites.Remove(kvp.Key);
            rem.Add(kvp.Key);
        }
        foreach (var pos in rem)
        {
            sprites.Remove(pos);
        }
    }

    public void UpdateActiveLayer(int idx)
    {
        // if (activeLayer == idx)
            // return;
        foreach (var kvp in sprites)
        {
            // mapRoot.RemoveChild(kvp.Value);
        }
        activeLayer = idx;
        CallDeferred(MethodName.UpdateLayers);
    }

    private void _RoomListSelected(Vector2 mousePosition, long mouseButtonIndex)
    {
        TreeItem sel = roomList.GetSelected();
        int col = roomList.GetSelectedColumn();
        if (IsInstanceValid(sel))
        {
            var obj = sel.GetMetadata(0).AsGodotObject();
            if (IsInstanceValid(obj) && obj is PresetRoom room)
            {
                toolAddRoomPreset = room;
                toolButtons[(int)ToolType.Add].ButtonPressed = true;
            }
        }
    }

    private void _TreeSelected(Vector2 mousePosition, long mouseButtonIndex)
    {
        if (isTreeLoading)
        {
            return;
        }
        if (layerTree.GetRoot().IsSelected(1))
        {
            layout.layers.Add(new MapLayoutLayer());
            saveVersion++;
            EmitSignalOnMapEdited();
            // UpdateLayersTree();
            CallDeferred(MethodName.UpdateLayersTree);
            return;
        }
        TreeItem sel = layerTree.GetSelected();
        int col = layerTree.GetSelectedColumn();
        if (IsInstanceValid(sel) && layerTree.GetRoot() != sel)
        {
            if (col == 0)
            {
                int idx = sel.GetMetadata(0).AsInt32();
                // UpdateActiveLayer(idx);
                CallDeferred(MethodName.UpdateActiveLayer, idx);
            }
            else if (col == 1)
            {
                int idx = sel.GetMetadata(0).AsInt32();
                layout.layers.RemoveAt(idx);
                if (activeLayer == idx)
                    activeLayer = 0;
                saveVersion++;
                EmitSignalOnMapEdited();
                // UpdateLayersTree();
                CallDeferred(MethodName.UpdateLayersTree);
            }
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            destroyAction = new Callable(GetTree(), SceneTree.MethodName.Quit);
            if (saveVersion == 0)
                destroyAction.Call();
            else
                EmitSignalOnTryExit();
        }
    }

    public override void _GuiInput(InputEvent ev)
    {
        if (ev is InputEventMouseButton button)
        {
            switch (button.ButtonIndex)
            {
                case MouseButton.Left:
                {
                    if (button.Pressed && !button.ShiftPressed)
                    {
                        Vector2 pos = camera.GetGlobalMousePosition();
                        Vector2I ipos = (Vector2I)(pos / pixelSize).Round();
                        UseActiveTool(ipos);
                    }
                    AcceptEvent();
                    break;
                }
                case MouseButton.Right:
                {
                    if (button.Pressed)
                    {
                        contextPosition = camera.GetGlobalMousePosition();
                        contextMenu.Position = DisplayServer.MouseGetPosition();
                        contextMenu.Show();
                    }
                    AcceptEvent();
                    break;
                }
                case MouseButton.WheelUp:
                    if (button.Pressed)
                        camera.Zoom *= 1.1f;
                    AcceptEvent();
                    break;
                case MouseButton.WheelDown:
                    if (button.Pressed)
                        camera.Zoom /= 1.1f;
                    AcceptEvent();
                    break;
            }
        }
        else if (ev is InputEventMouseMotion motion)
        {
            Vector2 pos = camera.GetGlobalMousePosition();
            Vector2I ipos = (Vector2I)(pos / pixelSize).Round();
            if (sprites.TryGetValue(ipos, out var sprite))
                helpLabel.Text = ipos.ToString() + ' ' + sprite.RoomName;
            else
                helpLabel.Text = ipos.ToString();
            if (motion.ButtonMask.HasFlag(MouseButtonMask.Middle) || (motion.ButtonMask.HasFlag(MouseButtonMask.Left) && motion.ShiftPressed))
            {
                camera.Position -= motion.Relative / camera.Zoom;
                AcceptEvent();
            }
        }
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is InputEventKey key)
        {
            if (key.CtrlPressed && key.Pressed)
            {
                if (key.Keycode == Key.Z)
                {
                    Undo();
                    GetViewport().SetInputAsHandled();
                }
                else if (key.Keycode == Key.Y)
                {
                    Redo();
                    GetViewport().SetInputAsHandled();
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        Vector2 pos = camera.GetTargetPosition() / pixelSize.Y * roomSize;
        previewCamera.Position = new Vector3(pos.X, 500f, pos.Y);
        previewCamera.Size = roomSize / (pixelSize.Y / camera.GetViewportRect().Size.Y) / camera.Zoom.Y;
        grid.Position = camera.Position - camera.GetViewportRect().Size / camera.Zoom;
        grid.Size = camera.GetViewportRect().Size * 2 / camera.Zoom;
    }

    private void _SpriteAdded(Node node)
    {
        if (node is LayoutEditorObject2D sprite)
        {
            Vector2I ipos = sprite.LayerPosition;
            // sprites.Add(ipos, sprite);
        }
    }

    private void _SpriteRemoved(Node node)
    {
        if (node is LayoutEditorObject2D sprite)
        {
            Vector2I ipos = sprite.LayerPosition;
            // sprites.Remove(ipos);
        }
    }

    public void Undo()
    {
        if (undoRedoIndex <= 0)
            return;
        undoRedo[undoRedoIndex - 1].Value.Invoke();
        undoRedoIndex--;
        EmitSignalOnMapEdited();
        // UpdateLayers();
        CallDeferred(MethodName.UpdateLayers);
    }

    public void Redo()
    {
        if (undoRedoIndex >= undoRedo.Count)
            return;
        undoRedo[undoRedoIndex].Key.Invoke();
        undoRedoIndex++;
        EmitSignalOnMapEdited();
        // UpdateLayers();
        CallDeferred(MethodName.UpdateLayers);
    }

    public void AddAction(Action redo, Action undo)
    {
        while (undoRedoIndex < undoRedo.Count)
        {
            undoRedo.RemoveAt(undoRedoIndex);
        }
        undoRedo.Add(new KeyValuePair<Action, Action>(redo, undo));
        Redo();
    }

    public void ClearHistory()
    {
        for (int i = 0; i < spriteCache.Count; i++)
        {
            spriteCache[i].QueueFreeNow();
        }
        spriteCache.Clear();
        undoRedo.Clear();
        undoRedoIndex = 0;
        saveVersion = 0;
    }

    public void ConfirmDiscardCallback()
    {
        destroyAction.Call();
    }

    private void _ContextSelected(long idx)
    {
        switch (contextMenu.GetItemId((int)idx))
        {
            case 0: // delete
            {
                Vector2 pos = contextPosition;
                Vector2I iPos = (Vector2I)(pos / pixelSize).Round();
                if (sprites.TryGetValue(iPos, out var sprite))
                {
                    var layer = layout.layers[activeLayer];
                    AddAction(() => layer.layout.Remove(sprite.layoutRoom), () => layer.layout.Add(sprite.layoutRoom));
                    saveVersion++;
                    EmitSignalOnMapEdited();
                    // UpdateLayers();
                    CallDeferred(MethodName.UpdateLayers);
                }
                break;
            }
            case 1: // inspect
            {
                Vector2 pos = contextPosition;
                Vector2I iPos = (Vector2I)(pos / pixelSize).Round();
                if (sprites.TryGetValue(iPos, out var sprite))
                {
                    EmitSignalInspectRoom(sprite);
                }
                else
                {
                    EmitSignalInspectRoom(null);
                }
                break;
            }
            case 2: // rotate cw
            {
                Vector2 pos = contextPosition;
                Vector2I iPos = (Vector2I)(pos / pixelSize).Round();
                if (sprites.TryGetValue(iPos, out var sprite))
                {
                    AddAction(() => sprite.layoutRoom.AddRotation(), () => sprite.layoutRoom.SubRotation());
                    saveVersion++;
                    EmitSignalOnMapEdited();
                    // UpdateLayers();
                    CallDeferred(MethodName.UpdateLayers);
                }
                break;
            }
            case 3: // rotate ccw
            {
                Vector2 pos = contextPosition;
                Vector2I iPos = (Vector2I)(pos / pixelSize).Round();
                if (sprites.TryGetValue(iPos, out var sprite))
                {
                    AddAction(() => sprite.layoutRoom.SubRotation(), () => sprite.layoutRoom.AddRotation());
                    saveVersion++;
                    EmitSignalOnMapEdited();
                    // UpdateLayers();
                    CallDeferred(MethodName.UpdateLayers);
                }
                break;
            }
        }
    }

    public void FileMenuCallback(long idx)
    {
        switch (idx)
        {
            case 0: // exit to menu
                destroyAction = new Callable(MenuManager.Instance, MenuManager.MethodName.LoadMenu);
                if (saveVersion == 0)
                    destroyAction.CallDeferred();
                else
                    EmitSignalOnTryExit();
                break;
            case 2: // save file
                EmitSignalSaveFile();
                break;
            case 1: // load file
                destroyAction = Callable.From(EmitSignalOpenFile);
                if (saveVersion == 0)
                    destroyAction.CallDeferred();
                else
                    EmitSignalOnTryExit();
                break;
        }
    }

    public void EditMenuCallback(long idx)
    {
        switch (idx)
        {
            case 0: // help
                EmitSignalOpenHelp();
                break;
            case 1: // undo
                Undo();
                break;
            case 2: // redo
                Redo();
                break;
        }
    }

    public void SaveLayoutToFile(string path = "user://layout.json")
    {
        FileAccess access = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (IsInstanceValid(access))
        {
            activeLayer = -1;
            foreach (var sp in sprites)
            {
                sp.Value.QueueFree();
            }
            sprites.Clear();
            access.StoreString(JsonSerializer.Serialize(layout));
            access.Close();
            ClearHistory();
            EmitSignalOnMapEdited();
        }
    }

    public void LoadLayerFromFile(string path = "user://layout.json")
    {
        FileAccess access = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (IsInstanceValid(access))
        {
            activeLayer = -1;
            foreach (var sp in sprites)
            {
                sp.Value.QueueFree();
            }
            sprites.Clear();
            layout = JsonSerializer.Deserialize<MapLayout>(access.GetAsText());
            access.Close();
            ClearHistory();
            EmitSignalOnMapEdited();
            CallDeferred(MethodName.UpdateLayersTree);
            CallDeferred(MethodName.UpdateActiveLayer, 0);
        }
    }

    public PresetRoom FindRoomByName(string name)
    {
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].ResourceName == name)
            {
                return rooms[i];
            }
        }
        return null;
    }
}