using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class LayoutEditorPreview : Control
{
    [Export]
    public Node3D camRig;
    [Export]
    public Camera3D camera;
    [Export]
    public Node3D mapRoot;
    [Export]
    public LayoutEditor editor;

    public Dictionary<Vector3I, Node3D> rooms = new Dictionary<Vector3I, Node3D>();
    public HashSet<Vector3I> positions = new HashSet<Vector3I>();

    public override void _Ready()
    {
        VisibilityChanged += RegenMap;
    }

    private void RegenMap()
    {
        foreach (var room in rooms)
        {
            // room.QueueFree();
        }
        // rooms.Clear();
        // if (IsVisibleInTree())
        {
            positions.Clear();
            editor.UpdateActiveLayer(editor.activeLayer);
            for (int i = 0; i < editor.layout.layers.Count; i++)
            {
                MapLayoutLayer layer = editor.layout.layers[i];
                foreach (var item in layer.layout)
                {
                    Vector3I iPos = new Vector3I(item.x, i, item.z);
                    PresetRoom room = editor.FindRoomByName(item.name);
                    if (IsInstanceValid(room))
                    {
                        positions.Add(iPos);
                        if (rooms.ContainsKey(iPos))
                        {
                            if (IsInstanceValid(rooms[iPos]) && room.SceneCached.ResourcePath == rooms[iPos].SceneFilePath)
                            {
                                // found an existing room
                                Node3D node = rooms[iPos];
                                node.Position = new Vector3(item.x, i, item.z) * editor.roomSize;
                                node.RotationDegrees = Vector3.Up * -90f * item.rotation;
                                continue;
                            }
                            else
                            {
                                // found an invalid room
                                if (IsInstanceValid(rooms[iPos]))
                                {
                                    rooms[iPos].QueueFreeNow();
                                }
                                rooms.Remove(iPos);
                            }
                        }
                        {
                            // no existing room
                            Node3D node = room.SceneCached.Instantiate<Node3D>();
                            node.Position = new Vector3(item.x, i, item.z) * editor.roomSize;
                            node.RotationDegrees = Vector3.Up * -90f * item.rotation;
                            mapRoot.AddChild(node);
                            rooms.Add(iPos, node);
                        }
                    }
                }
            }
            foreach (var kvp in rooms)
            {
                if (IsInstanceValid(kvp.Value) && !positions.Contains(kvp.Key))
                {
                    kvp.Value.QueueFreeNow();
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;
        Vector2 movement = Input.GetVector("player_left", "player_right", "player_forward", "player_backward") * 10f;
        float updown = Input.GetAxis("player_sneak", "player_jump") * 10f;
        camRig.Position += camRig.Basis.Z * (float)delta * movement.Y + camRig.Basis.X * (float)delta * movement.X + camRig.Basis.Y * (float)delta * updown;
    }

    public override void _GuiInput(InputEvent ev)
    {
        if (ev is InputEventMouseButton button)
        {
            switch (button.ButtonIndex)
            {
                case MouseButton.WheelUp:
                    if (button.Pressed)
                        camRig.Position -= camera.GlobalBasis.Z * editor.roomSize / 4f;
                    AcceptEvent();
                    break;
                case MouseButton.WheelDown:
                    if (button.Pressed)
                        camRig.Position += camera.GlobalBasis.Z * editor.roomSize / 4f;
                    AcceptEvent();
                    break;
            }
        }
        else if (ev is InputEventMouseMotion motion)
        {
            if (motion.ButtonMask.HasFlag(MouseButtonMask.Right))
            {
                Vector2 mouseMotion = motion.ScreenRelative * Mathf.DegToRad(22.5f) * -0.01f;
                camRig.Rotation += new Vector3(0, mouseMotion.X, 0);
                camera.Rotation += new Vector3(mouseMotion.Y, 0, 0);
                camera.Rotation = new Vector3(Mathf.Clamp(camera.Rotation.X, -1.55f, 1.55f), camera.Rotation.Y, camera.Rotation.Z);
                AcceptEvent();
            }
        }
    }
}