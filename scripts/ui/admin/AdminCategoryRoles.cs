using System;
using System.Linq;
using Godot;

public partial class AdminCategoryRoles : AdminCategoryCommandBase
{
    public PlayerRole[] PlayerRoles => IInitScript.Instance.Data.RoleLookup.Values.ToArray();
    
    [Export]
    public ItemList RoleList;
    
    [Export]
    public BaseButton UseSpawnpoint;
    
    public enum Container
    {
        Commands,
    }

    public override void _Ready()
    {
        base._Ready();
        
        RoleList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        RoleList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        foreach (PlayerRole role in PlayerRoles)
        {
            if (role != null)
            {
                int index = RoleList.AddItem(role?.DisplayName, role?.HintIcon); // Make this receive the icons instead
            }
        }
    }
}