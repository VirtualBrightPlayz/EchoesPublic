using System.Collections.Generic;
using Godot;

public class MapLayout
{
    public List<MapLayoutLayer> layers { get; set; } = new List<MapLayoutLayer>();
}

public class MapLayoutLayer
{
    public List<MapLayoutRoom> layout { get; set; } = new List<MapLayoutRoom>();
}

public class MapLayoutRoom
{
    public const string KeyAllowedRooms = "allowed_rooms";

    public int x { get; set; }
    public int z { get; set; }
    public int rotation { get; set; }
    public int type { get; set; }
    public string name { get; set; }
    public Dictionary<string, object> keyvalues { get; set; }

    public MapLayoutRoom()
    {
        x = 0;
        z = 0;
        rotation = 0;
        type = 0;
        name = string.Empty;
        keyvalues = new Dictionary<string, object>();
    }

    public MapLayoutRoom(Vector2I pos, Sprite2D sprite)
    {
        x = pos.X;
        z = pos.Y;
        rotation = Mathf.RoundToInt(sprite.RotationDegrees / 90f) % 4;
        type = sprite.GetMeta("room_type", 0).AsInt32();
        name = sprite.GetMeta("room_name", string.Empty).AsString();
        keyvalues = new Dictionary<string, object>();
    }

    public void AddRotation()
    {
        rotation = (rotation + 1) % 4;
    }

    public void SubRotation()
    {
        rotation = (rotation + 3) % 4;
    }

    public void ApplyToSprite2D(Vector2 pixelSize, Texture2D texture, Sprite2D sprite, PresetRoom room)
    {
        if (GodotObject.IsInstanceValid(room.layoutIconOverride))
            sprite.Texture = room.layoutIconOverride;
        else
            sprite.Texture = texture;
        sprite.Scale = pixelSize / sprite.Texture.GetSize() * room.gridRect.Size;
        sprite.Position = new Vector2(x, z) * pixelSize;
        var rect = room.gridRect;
        sprite.Offset = rect.Position * sprite.Texture.GetSize() / rect.Size - sprite.Texture.GetSize() / 2f / rect.Size;
        sprite.RotationDegrees = rotation * 90f;
        sprite.SetMeta("room_position", new Vector2I(x, z));
        sprite.SetMeta("room_type", type);
        sprite.SetMeta("room_name", name);
    }
}