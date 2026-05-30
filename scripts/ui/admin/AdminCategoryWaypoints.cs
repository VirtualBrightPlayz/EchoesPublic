using System;
using System.Linq;
using Godot;

public partial class AdminCategoryWaypoints : Node
{
    public AdminHUD Admin => GetParent().GetMeta(AdminHUD.META_NAME).As<AdminHUD>();
    
    [Export]
    public Control container;

    public override void _Ready()
    {
        container.VisibilityChanged += VisChange;
        VisChange();
    }

    private void VisChange()
    {
        if (container.IsVisibleInTree())
        {
            foreach (var ch in container.GetChildren())
            {
                ch.QueueFree();
            }
            var tpPoints = GetTree().GetNodesInGroup("dev_teleport"); // Make waypoints receive zone or something, this is kinda messy rn
            foreach (var pt in tpPoints)
            {
                if (pt is Node3D n3d)
                {
                    var btn = new Button();
                    btn.Text = pt.Name;
                    container.AddChild(btn);
                    float x = n3d.GlobalPosition.X;
                    float y = n3d.GlobalPosition.Y;
                    float z = n3d.GlobalPosition.Z;
                    btn.Pressed += () => Admin.RunWithSelectedPlayers($"teleport_xyz {{0}} {x} {y} {z}");
                }
            }
        }
    }
}