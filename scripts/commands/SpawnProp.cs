using Godot;
using Godot.Collections;

public class SpawnProp : SimpleAdminPlayerCommandBase
{
    public override string Command { get; } = "spawnprop";

    public override string[] Alias { get; } = new[] { "gmod", "sprop", "prop" };

    public override string CommandDescription { get; } = "Spawns a Prop";

    public override PlayerMode Target { get; } = PlayerMode.SelfTarget;
    
    public override bool Execute(PlayerConsole console, NetworkPlayer player, string[] args, out string response)
    {
        if (args.Length == 0)
        {
            response = "Item not spawned";
            return false;
        }
        PackedScene spawnItem = null;
        if (int.TryParse(args[0], out int id) && id >= 0 && id < ItemManager.Instance.Data.Props.Length)
        {
            spawnItem = ItemManager.Instance.Data.Props[id];
        }
        if (spawnItem == null)
        {
            foreach (var item in ItemManager.Instance.Data.Props)
            {
                if (item.ResourceName.StartsWith(args[0], System.StringComparison.OrdinalIgnoreCase))
                {
                    spawnItem = item;
                    break;
                }
            }
        }
        if (spawnItem == null)
        {
            response = "Prop not found";
            return false;
        }
        Node3D prop = spawnItem.Instantiate() as Node3D;
        if (!GodotObject.IsInstanceValid(prop))
        {
            response = "Prop is invalid";
            return false;
        }
        Node3D root = prop;
        if (prop is not PhysicsProp3D)
        {
            Array<Node> props = prop.FindChildren("*", string.Empty, true, false);
            foreach (Node child in props)
            {
                if (child is PhysicsProp3D prop3d)
                {
                    if (!GodotObject.IsInstanceValid(prop3d.Parent))
                    {
                        prop = prop3d;
                        break;
                    }
                }
            }
        }
        if (player.ActiveController is FPController fpController && player.TryGetAbility(out GrabbingAbility ability))
        {
            Vector3 newPos = fpController.GlobalPosition;
            ability.rayCast.ForceRaycastUpdate();
            if (ability.rayCast.IsColliding())
            {
                newPos = ability.rayCast.GetCollisionPoint();
            }
            else
            {
                newPos = ability.rayCast.ToGlobal(ability.rayCast.TargetPosition);
            }
            newPos.Y += 0.5f;
            root.Position = newPos;
            ItemManager.Instance.PropSpawnNode.AddChild(root, true);
            response = "Done";
            return true;
        }
        else
        {
            root.Position = player.ActiveController.Root.GlobalPosition;
            ItemManager.Instance.PropSpawnNode.AddChild(root, true);
            response = "Prop spawned, but at 0, 0, 0 because player controller is not a First Person Controller.";
            return false;
        }
    }
}