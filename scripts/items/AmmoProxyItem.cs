using Godot;

[GlobalClass]
public partial class AmmoProxyItem : ItemPreset
{
    [Export]
    public AmmoItemPreset BasePreset;
    [Export]
    public int AmmoAmount = 50;

    public override int Id => BasePreset.Id;

    public override ItemObject SpawnNew()
    {
        var ammo = BasePreset.SpawnNew();
        (ammo.model as AmmoItem).amount = AmmoAmount;
        return ammo;
    }
}
