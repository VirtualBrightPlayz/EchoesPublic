using System.Linq;
using Godot;

public partial class PlayerNetTest : Node
{
    [Export]
    public float amount;
    [Export]
    public NodePath path;

    public override void _Ready()
    {
        var obj = new DamageInfo(amount, path, this, DamageType.Unknown);
        GD.PrintS("Initial Object:", obj);
        GD.PrintS("Network Dictionary:", string.Join(',', DamageInfo.ToNetwork(obj).Select(x => x.ToString())));
        var obj2 = new DamageInfo(DamageInfo.ToNetwork(obj));
        GD.PrintS("Serialized Object:", obj2);
        if (obj == obj2)
        {
            GD.PrintErr("Both objects are the same, test failed.");
        }
        else if (obj.Amount == obj2.Amount && obj.SourceAbsolutePath == obj2.SourceAbsolutePath)
        {
            GD.PrintRich("[color=green]Both objects share the same values, test passed.[/color]");
        }
        else
        {
            GD.PrintErr("Both objects are completely different, test failed.");
        }
    }
}
