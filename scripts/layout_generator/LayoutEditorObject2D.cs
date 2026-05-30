using System.Collections.Generic;
using Godot;

public partial class LayoutEditorObject2D : Sprite2D
{
    [Export]
    public Sprite2D icon;
    [Export]
    public Vector2I LayerPosition;
    [Export]
    public string RoomName;
    [Export]
    public PresetRoom.RoomClass RoomType;
    public MapLayoutRoom layoutRoom;
    public PresetRoom preset;

    public void Setup(LayoutEditor editor, MapLayoutRoom room)
    {
        Visible = true;
        layoutRoom = room;
        if (!IsInstanceValid(preset) || room.name != RoomName)
        {
            preset = editor.FindRoomByName(room.name);
        }
        Centered = false;
        SelfModulate = new Color(1f, 1f, 1f, 0.5f);
        RoomName = room.name;
        if (!IsInstanceValid(preset))
        {
            return;
        }
        RoomType = preset.type;
        LayerPosition = new Vector2I(room.x, room.z);
        Position = LayerPosition * editor.pixelSize;
        RotationDegrees = room.rotation * 90f;
        if (IsInstanceValid(preset.layoutIconOverride))
            Texture = preset.layoutIconOverride;
        else
            Texture = editor.roomTextures[(int)preset.type];
        Scale = editor.pixelSize / Texture.GetSize() * (Vector2)preset.gridRect.Size;
        var rect = (Rect2)preset.gridRect;
        Offset = rect.Position * Texture.GetSize() / rect.Size - Texture.GetSize() / 2f / rect.Size;
        if (IsInstanceValid(preset.layoutIcon))
        {
            icon.Visible = true;
            icon.Centered = false;
            icon.Texture = preset.layoutIcon;
            icon.Scale = editor.pixelSize / icon.Texture.GetSize() * preset.gridRect.Size / Scale;
            icon.Offset = rect.Position * icon.Texture.GetSize() / rect.Size - icon.Texture.GetSize() / 2f / rect.Size;
        }
        else
            icon.Visible = false;
    }

    public Dictionary<string, (RoomPropertyData.PropType, object)> GetProps()
    {
        var dict = new Dictionary<string, (RoomPropertyData.PropType, object)>();
        if (IsInstanceValid(preset))
        {
            if (preset.isGeneric)
            {
                dict.Add(MapLayoutRoom.KeyAllowedRooms, (RoomPropertyData.PropType.String, string.Empty));
            }
            foreach (var data in preset.templateData)
            {
                string value = data.value;
                if (preset.templateDefaultValues.ContainsKey(data.key))
                {
                    value = preset.templateDefaultValues[data.key];
                }
                if (layoutRoom.keyvalues.ContainsKey(data.key))
                {
                    value = layoutRoom.keyvalues[data.key]?.ToString();
                }
                switch (data.type)
                {
                    case RoomPropertyData.PropType.String:
                        dict.Add(data.key, (data.type, value));
                        break;
                    case RoomPropertyData.PropType.Int:
                        dict.Add(data.key, (data.type, int.Parse(value)));
                        break;
                    case RoomPropertyData.PropType.Float:
                        dict.Add(data.key, (data.type, float.Parse(value)));
                        break;
                    case RoomPropertyData.PropType.Bool:
                        dict.Add(data.key, (data.type, bool.Parse(value)));
                        break;
                }
            }
        }
        return dict;
    }
}