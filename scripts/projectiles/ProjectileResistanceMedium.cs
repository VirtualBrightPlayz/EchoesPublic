using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class ProjectileResistanceMedium : Resource
{
	[Export, Description("Penetration cost applied to projectiles in this medium each second. Unit: cost/cm")]
	public double PenetrationCost = 0f;

	[Export, Description("Damage multiplier applied to the projectile in this medium each second. Unit: 1/cm")]
	public double DamageMultiplier = 1f;

	[Export, Description("Gravity acceleration multiplier that is applied to projectiles in this medium.")]
	public double GravityMultiplier = 1.0f;

	[Export, Description("Speed acceleration multiplier that is applied to projectiles in this medium.")]
	public double SpeedMultiplier = 1.0f;

	[Export, Description("Name used for debugging")]
	public string DebugName = "Air";

	[Export, Description("Color used for debugging")]
	public Color DebugColor = Colors.Green;
}
