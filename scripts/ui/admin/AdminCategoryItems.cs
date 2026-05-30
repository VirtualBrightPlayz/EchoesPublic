using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class AdminCategoryItems : AdminCategoryCommandBase
{
    [Export]
    public ItemList ItemList;

    public ItemPreset[] ItemPresets => ItemManager.Instance.Data.ItemPresets;

    public enum Container
    {
        Commands,
    }

    public override void _Ready()
    {
        base._Ready();

        ItemList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        ItemList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        foreach (ItemPreset item in ItemPresets)
        {
            if (item != null)
            {
                int index = ItemList.AddItem(item?.ResourceName, item?.Icon);
            }
        }
    }
}