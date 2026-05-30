using Godot;

[GlobalClass]
[Tool]
public partial class MapRoom : Node3D
{
    [Export]
    public PresetRoom.RoomClass type;
    [Export]
    public PresetRoom.RoomSize size;
    [Export]
    public PresetRoom room;
    [Export]
    public int stage = -1;
    [Export]
    public string[] allowedRooms = new string[0];

    private MeshInstance3D spawnedMesh;
    private ImmediateMesh mesh;
    private StandardMaterial3D material;
    private Label3D label;
    public MapLayoutRoom layoutRoom;
    public Vector3I layoutPosition;
    public Vector2I layoutPosition2d => new Vector2I(layoutPosition.X, layoutPosition.Z);
    public int layoutRotation;

    public override void _EnterTree()
    {
        SpawnMesh();
    }

    public override void _Ready()
    {
        Cleanup();
        SpawnMesh();
    }

    public override void _ExitTree()
    {
        Cleanup();
    }

    public void Cleanup()
    {
        if (IsInstanceValid(spawnedMesh))
        {
            spawnedMesh.QueueFree();
            spawnedMesh = null;
        }
        if (IsInstanceValid(label))
        {
            label.QueueFree();
            label = null;
        }
    }

    public void SetupFromLayout(MapLayoutRoom layout, PresetRoom preset, Vector3I pos, int rot)
    {
        layoutRoom = layout;
        layoutPosition = pos;
        layoutRotation = rot % 4;
        room = preset;
        type = (PresetRoom.RoomClass)layoutRoom.type;
        size = room.size;
        stage = room.stage;
        if (layoutRoom.keyvalues.TryGetValue(MapLayoutRoom.KeyAllowedRooms, out var allowed))
        {
            allowedRooms = allowed?.ToString()?.Split(',') ?? new string[0];
        }
    }

    public Rect2I GetRect(float mapSize, PresetRoom r)
    {
        var pos = new Vector2I((int)(Position.X / mapSize), (int)(Position.Z / mapSize));
        int rot = Mathf.RoundToInt(RotationDegrees.Y / -90f) % 4;
        if (!IsInstanceValid(r))
        {
            r = room;
        }
        {
            pos = new Vector2I(layoutPosition.X, layoutPosition.Z);
            rot = layoutRotation;
        }
        var ipos = r.gridRect.Position;
        var siz = r.gridRect.Size;
        switch (rot)
        {
            case 0:
                break;
            case 1:
                ipos = new Vector2I(ipos.Y, -ipos.X);
                siz = new Vector2I(siz.Y, -siz.X);
                break;
            case 2:
                ipos = -ipos;
                siz = -siz;
                break;
            case 3:
                ipos = new Vector2I(-ipos.Y, ipos.X);
                siz = new Vector2I(-siz.Y, siz.X);
                break;
        }
        Rect2I rect = new Rect2I(ipos, siz);
        rect.Position += pos;
        return rect;
    }

    public void SpawnMesh()
    {
        if (!Engine.IsEditorHint())
            return;
        spawnedMesh = new MeshInstance3D();
        mesh = new ImmediateMesh();
        material = new StandardMaterial3D();
        material.VertexColorUseAsAlbedo = true;
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
        mesh.SurfaceSetColor(Colors.Red);
        mesh.SurfaceAddVertex(new Vector3(0, 1, 0));
        mesh.SurfaceSetColor(Colors.Red);
        mesh.SurfaceAddVertex(new Vector3(0, 0, 0));
        mesh.SurfaceEnd();
        spawnedMesh.Mesh = mesh;
        CallDeferred(Node.MethodName.AddChild, spawnedMesh);
        
        label = new Label3D();
        if (IsInstanceValid(room))
        {
            label.Text = $"{room.ResourcePath}\n{layoutPosition}\n{layoutRotation}";
        }
        else
        {
            label.Text = $"type={type}\nsize={size}\nstage={stage}";
        }
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        // label.NoDepthTest = true;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        CallDeferred(Node.MethodName.AddChild, label);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            Cleanup();
        }
        if (what == NotificationEditorPostSave)
        {
            SpawnMesh();
        }
    }
}
