using Godot;

[GlobalClass]
public partial class BrigMapGenerator : Marker3D
{
    [Export]
    public ulong mapSeed;
    [Export]
    public float mapSize = 6.935f;
    public RandomNumberGenerator rng;

    [ExportGroup("Floors")]
    [Export]
    public PackedScene floorTop;
    [Export]
    public PackedScene floorBottom;
    [Export]
    public PackedScene[] floors;
    [Export]
    public int minFloors = 2;
    [Export]
    public int maxFloors = 8;

    [ExportGroup("Sub Rooms")]
    [Export]
    public PackedScene[] subRooms;

    public override void _Ready()
    {
        base._Ready();
        Generate();
    }

    public void Generate()
    {
        rng = new RandomNumberGenerator();
        int count = rng.RandiRange(minFloors, maxFloors);
        for (int i = 0; i < count; i++)
        {
            var scn = floors[rng.Randi() % floors.Length];
            if (i == 0)
            {
                scn = floorTop;
            }
            else if (i + 1 >= count)
            {
                scn = floorBottom;
            }
            var floorInst = scn.Instantiate<Node3D>();
            AddChild(floorInst, true);
            floorInst.GlobalPosition = GlobalPosition - new Vector3(0f, mapSize * i, 0f);
            floorInst.GlobalRotation = GlobalRotation;
            var roomPoints = floorInst.FindChildren("*", nameof(Marker3D));
            foreach (Marker3D point in roomPoints)
            {
                if (!point.Visible)
                    continue;
                var roomScn = subRooms[rng.Randi() % subRooms.Length];
                var roomInst = roomScn.Instantiate<Node3D>();
                point.AddChild(roomInst, true);
                // roomInst.Rotation = Vector3.Zero;
                // roomInst.GlobalTransform = point.GlobalTransform;
                // roomInst.GlobalRotation = point.GlobalRotation + Vector3.Up * Mathf.Pi;
            }
        }
    }
}