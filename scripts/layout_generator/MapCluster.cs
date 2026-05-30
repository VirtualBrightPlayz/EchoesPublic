using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
[Tool]
public partial class MapCluster : Node3D
{
    public struct Cell
    {
        public ERoomDirectionFlags directionFlags;

        public Cell()
        {
            directionFlags = 0;
        }
    }

    [Export]
    public int stage;

    [Export]
    public Vector3 mapScale = Vector3.One * 20.8f;
    [Export(PropertyHint.File)]
    public string file;
#if TOOLS
    [ExportToolButton("Import")]
    public Callable Import => Callable.From(ImportFromFile);
#endif

    public readonly Vector3I[] directions = new Vector3I[]
    {
        Vector3I.Forward,
        Vector3I.Right,
        Vector3I.Back,
        Vector3I.Left,
        // Vector3I.Up,
        // Vector3I.Down,
    };

    public readonly ERoomDirectionFlags[] roomDirectionFlags = new ERoomDirectionFlags[]
    {
        ERoomDirectionFlags.NegativeZ,
        ERoomDirectionFlags.PositiveX,
        ERoomDirectionFlags.PositiveZ,
        ERoomDirectionFlags.NegativeX,
    };

    private List<KeyValuePair<Vector3I, Color>> importedCells = new List<KeyValuePair<Vector3I, Color>>();

    private int GetIndexOfPosition(Vector3I pos)
    {
        for (int i = 0; i < importedCells.Count; i++)
        {
            if (importedCells[i].Key == pos)
                return i;
        }
        return -1;
    }

#if TOOLS
    public void ImportFromFile()
    {
        var undo = EditorInterface.Singleton.GetEditorUndoRedo();
        undo.CreateAction("Import Map");
        GD.Print("ImportFromFile");
        importedCells.Clear();
        Color door = Color.FromHtml("ff0");
        Color generic = Color.FromHtml("f00");
        if (FileAccess.FileExists(file))
        {
            string[] lines = FileAccess.GetFileAsString(file).ReplaceLineEndings("\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith('#'))
                    continue;
                string[] vals = lines[i].Split(' ');
                if (vals.Length > 3)
                {
                    int x = int.Parse(vals[0]);
                    int y = int.Parse(vals[1]);
                    int z = int.Parse(vals[2]);
                    string hex = vals[3];
                    Color color = Color.FromHtml(hex);
                    importedCells.Add(new KeyValuePair<Vector3I, Color>(new Vector3I(x, z, y), color));
                }
            }
            for (int i = 0; i < importedCells.Count; i++)
            {
                bool isGeneric = IsEqualApprox(importedCells[i].Value, generic);
                if (isGeneric)
                {
                    ERoomDirectionFlags flags = 0;
                    for (int j = 0; j < directions.Length; j++)
                    {
                        int idx = GetIndexOfPosition(importedCells[i].Key + directions[j]);
                        if (idx == -1)
                            continue;
                        bool isDoor = IsEqualApprox(importedCells[idx].Value, door);
                        if (isDoor)
                            flags |= roomDirectionFlags[j];
                    }
                    MapRoom room = new MapRoom();
                    SpawnRoom(room, importedCells[i].Key, flags);
                    EditorUndoRedoAddChild(undo, room);
                    if (flags.HasFlag(ERoomDirectionFlags.PositiveX))
                    {
                        MapRoom doorX = new MapRoom();
                        SpawnDoor(doorX, importedCells[i].Key, true);
                        EditorUndoRedoAddChild(undo, doorX);
                    }
                    if (flags.HasFlag(ERoomDirectionFlags.PositiveZ))
                    {
                        MapRoom doorZ = new MapRoom();
                        SpawnDoor(doorZ, importedCells[i].Key, false);
                        EditorUndoRedoAddChild(undo, doorZ);
                    }
                }
            }
        }
        undo.CommitAction();
    }

    private void EditorUndoRedoAddChild(EditorUndoRedoManager undo, Node room)
    {
        undo.AddDoMethod(this, MethodName.AddChild, room, true);
        undo.AddDoProperty(room, MapRoom.PropertyName.Owner, this);
        undo.AddDoReference(room);
        undo.AddUndoMethod(this, MethodName.RemoveChild, room);
    }
#endif

    private MapRoom SpawnDoor(MapRoom room, Vector3I pos, bool isXAxis)
    {
        room.Name = $"Door_{pos.X}_{pos.Y}_{pos.Z}";
        room.Position = pos / 3 * mapScale + (isXAxis ? Vector3.Right : Vector3.Back) * mapScale / 2f;
        if (isXAxis)
            room.Rotation = Vector3.Up * Mathf.DegToRad(90f);
        return room;
    }

    private MapRoom SpawnRoom(MapRoom room, Vector3I pos, ERoomDirectionFlags flags)
    {
        room.Name = $"Room_{pos.X}_{pos.Y}_{pos.Z}";
        // AddChild(room, true);
        // room.Owner = this;
        room.Position = pos / 3 * mapScale;
        Dictionary<ERoomDirectionFlags, PresetRoom.RoomClass> lookup = new Dictionary<ERoomDirectionFlags, PresetRoom.RoomClass>()
        {
            { ERoomDirectionFlags.PositiveZ, PresetRoom.RoomClass.Endoff },
            { ERoomDirectionFlags.PositiveZ | ERoomDirectionFlags.PositiveX, PresetRoom.RoomClass.Corner },
            { ERoomDirectionFlags.PositiveZ | ERoomDirectionFlags.NegativeZ, PresetRoom.RoomClass.Hall },
            { ERoomDirectionFlags.PositiveZ | ERoomDirectionFlags.PositiveX | ERoomDirectionFlags.NegativeX, PresetRoom.RoomClass.TRoom },
            { ERoomDirectionFlags.AllDirections, PresetRoom.RoomClass.XRoom },
        };
        ERoomDirectionFlags flags2 = flags;
        for (int i = 0; i < 4; i++)
        {
            if (lookup.TryGetValue(flags2, out var roomType))
            {
                room.type = roomType;
                room.Rotation = Vector3.Up * Mathf.DegToRad(i * 90);
                room.stage = stage;
                break;
            }
            flags2 = RoomDirectionFlagsUtility.RotateRight(flags2);
        }
        return room;
    }

    public static bool IsEqualApprox(Color a, Color b, float t = 5f / 255f)
    {
        if (Mathf.IsEqualApprox(a.R, b.R, t) && Mathf.IsEqualApprox(a.G, b.G, t) && Mathf.IsEqualApprox(a.B, b.B, t))
        {
            return true;
        }
        return false;
    }

    public Node3D[] GetConnectors()
    {
        return GetChildren().Where(x => x.IsInGroup("map_connection") && x is Node3D).Select(x => (Node3D)x).ToArray();
    }

    public Node3D[] GetConnectorsUnused(int stage = -1)
    {
        return GetChildren().Where(x => x.IsInGroup("map_connection") && !x.GetMeta("map_used", false).AsBool() && (stage == -1 || x.GetMeta("map_stage", -1).AsInt32() == -1 || x.GetMeta("map_stage", -1).AsInt32() == stage) && x is Node3D).Select(x => (Node3D)x).ToArray();
    }

    public MapRoom[] GetRooms()
    {
        return FindChildren("*").Where(x => x is MapRoom).Select(x => (MapRoom)x).ToArray();
    }

    public Aabb GetAabb()
    {
        var box = new Aabb(GlobalPosition, Vector3.One);
        foreach (var room in GetRooms())
        {
            box = box.Expand(room.GlobalPosition);
        }
        foreach (var conn in GetConnectors())
        {
            // box = box.Expand(conn.GlobalPosition);
        }
        return box;
    }
}
