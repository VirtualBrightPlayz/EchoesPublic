using System.Linq;
using Godot;

public enum AmmoType : byte
{
    AmmoPistol = 0,
    AmmoRifle = 1,
    AmmoSmg = 2,
}

[GlobalClass]
public partial class AmmoItem : WorldItem
{
    public AmmoType TypeOfAmmo => (Item.Preset as AmmoItemPreset).TypeOfAmmo;
    [Export]
    public int amount;

    public override string ToString()
    {
        return $"{base.ToString()}\nAmount: {amount}";
    }
}
