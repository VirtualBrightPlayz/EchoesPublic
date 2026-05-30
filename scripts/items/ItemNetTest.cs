using Godot;

public partial class ItemNetTest : Node
{
    [Export]
    public InventoryItem item;

    public override void _Ready()
    {
        InventoryItem item2 = item.Copy() as InventoryItem;
        byte[] arr = item2.ToBytes();
        GD.PrintS(string.Join(",", arr));
        // item2.Timer = 1f;
        GD.PrintS(item2.ToString());
        item2.FromBytes(arr);
        GD.PrintS(item2.ToString());
    }
}
