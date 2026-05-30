using System.Linq;
using Godot;

[GlobalClass]
public partial class AmmoItemPreset : ItemPreset
{
	[Export]
	public AmmoType TypeOfAmmo = AmmoType.AmmoPistol;
}
