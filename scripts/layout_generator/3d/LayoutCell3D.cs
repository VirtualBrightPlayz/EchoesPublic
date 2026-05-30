using Godot;

public partial class LayoutCell3D : Node3D
{
	public bool IsGenerated { get; private set; }
    public Vector3I GridPosition { get; private set; }
    public int GridRotation { get; private set; }
    public LayoutGrid3D OwningLayout { get; private set; }
    public int Zone { get; private set; } // Used for scene selection and unique node seeds
    public PackedScene RoomSceneResource { get; private set; }
    public Node3D RoomSceneRootNode { get; private set; }

    public LayoutCell3D(LayoutGrid3D owningLayout, Vector3I position)
    {
        this.OwningLayout = owningLayout;
        this.GridPosition = position;
        this.Name = $"{owningLayout.Name}_Cell_X{position.X}_Y{position.Y}";
    }
}