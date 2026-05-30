using Godot;

[GlobalClass]
public partial class RoomVariant : Node
{
    [Signal]
    public delegate void VariantSelectedEventHandler(long id);

    [Export]
    public float cellSize = 20.8f;
    [Export]
    public Node3D[] variants = new Node3D[0];

    public override void _EnterTree()
    {
        if (IsInstanceValid(RoundManager.Instance))
        {
            RoundManager.Instance.OnSeed += Setup;
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(RoundManager.Instance))
        {
            RoundManager.Instance.OnSeed -= Setup;
        }
    }

    public override void _Ready()
    {
        if (IsInstanceValid(RoundManager.Instance))
        {
            Setup(RoundManager.Instance.mapSeed);
        }
        else
        {
            Setup(0);
        }
    }

    public void Setup(ulong seed)
    {
        // GD.Print(RoundManager.Instance.mapSeed);
        Node3D root = GetParent<Node3D>();
        Vector3 pos = root.GlobalPosition;
        pos /= cellSize;
        Vector3I iPos = (Vector3I)pos;
        // iPos = iPos * iPos;
        long output = (long)seed ^ iPos.X ^ iPos.Y ^ iPos.Z;
        EmitSignalVariantSelected(output);
        if (variants.Length == 0)
            return;
        for (int i = 0; i < variants.Length; i++)
        {
            if (IsInstanceValid(variants[i]))
            {
                variants[i].Visible = false;
                variants[i].ProcessMode = ProcessModeEnum.Disabled;
            }
        }
        long id = Mathf.Abs((int)output) % variants.Length;
        if (IsInstanceValid(variants[id]))
        {
            variants[id].Visible = true;
            variants[id].ProcessMode = ProcessModeEnum.Inherit;
        }
        // RandomNumberGenerator rng = new RandomNumberGenerator();
        // rng.Seed = (ulong)output;
    }
}