using Godot;

[GlobalClass]
public partial class Scp420J : InventoryItem
{
    /*
    public override void PrimaryPress(NetworkPlayer player)
    {
        if (!player.Multiplayer.IsServer())
            return;
        player.InventorySerials[player.EquippedItemIndex] = -1;
        player.EquippedItemIndex = -1;
		ItemManager.Instance.DeleteItem(this);
    }
    */
}