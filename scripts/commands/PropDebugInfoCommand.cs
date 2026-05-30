using System;
using System.Linq;

public class PropDebugInfoCommand : SimpleGameCommandBase
{
    public override string Command => "PropDebugInfo";
    public override string[] Alias => Array.Empty<string>();
    public override string CommandDescription => "Shows debugging information.";

    public override bool Execute(string[] args, out string response)
    {
        var props = PhysicsProp3D.PhysicsProps.Where(x => !x.Sleeping);
        var physicsProp3Ds = props as PhysicsProp3D[] ?? props.ToArray();
        response = string.Join("\n", physicsProp3Ds.Select(x => x.GetPath().ToString() + "Position: " + x.GlobalPosition.ToString() + ", Rotation: " + x.GlobalRotation.ToString()));
        response += $"\nDebugInfo\nItems: {physicsProp3Ds.Count()}\nTotal: {PhysicsProp3D.PhysicsProps.Count}";
        // response += string.Join("\n", ItemManager.Instance.Items.Select(x => x.Value.model.ToString() + x.Value.model.GlobalPosition.ToString()));
        return true;
    }
}