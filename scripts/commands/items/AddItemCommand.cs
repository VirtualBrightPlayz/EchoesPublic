using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class AddItemCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "AddItem";

    public override string[] Alias { get; } = [];

    public override string CommandDescription => "Adds an item to the players inventory";

    public override string ButtonLabel => "Add Item";

    public override Type Category => typeof(AdminCategoryItems);

    public override string Container => nameof(AdminCategoryItems.Container.Commands);
    
    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        if (args.Length < 2)
        {
            response = "Wrong syntax.";
            return false;
        }
        
        HashSet<NetworkPlayer> players = args.Length > 0  ? CommandHelper.ParsePlayersArgument(args[0]) : [console.Player];
        
        if (players.Count == 0)
        {
            response = "No suitable players.";
            return false;
        }
        
        ItemPreset itemPreset = null;
        
        if (int.TryParse(args[1], out int id))
        {
            itemPreset = ItemManager.Instance.Data.ItemPresets.FirstOrDefault(item => item.Id == id);
        }
        else
        {
            itemPreset = ItemManager.Instance.Data.ItemPresets.FirstOrDefault(item => item.ResourceName.StartsWith(args[1], System.StringComparison.OrdinalIgnoreCase));
        }
        
        if (itemPreset == null)
        {
            response = "Item not found";
            return false;
        }

        // Separate code for adding ammo 
        if (itemPreset is AmmoItemPreset ammo)
        {
            AmmoProxyItem ammoItem = new AmmoProxyItem();
            ammoItem.BasePreset = ammo;
            ammoItem.AmmoAmount = 100;
            ammoItem.ResourceName = ammo.ResourceName;
            itemPreset = ammoItem;
        }
        
        ItemObject itemObj = itemPreset.SpawnNew();

        foreach (NetworkPlayer player in players)
        {
            if (!player.TryGetAbility(out InventoryAbility inventory))
            {
                continue;
            }
            
            itemObj.SV_Teleport(player.PlayerPosition + Vector3.Up, player.PlayerRotation);
            itemObj.SV_Pickup(player, false);
        }
        
        response = $"Item {itemPreset?.ResourceName} added for {string.Join(", ", players.Select(p => p.username))}";
        return true;
    }
    
    public override void OnPressed(AdminHUD admin)
    {
        AdminCategoryItems category = admin.GetActiveCategory<AdminCategoryItems>();
        
        if (category == null)
        {
            return;
        }
        int[] selectedItem = category.ItemList.GetSelectedItems();

        if (selectedItem.Length == 0)
        {
            admin.RunWithSelectedPlayers($"additem {{0}}");
        }
        
        admin.RunWithSelectedPlayers($"additem {{0}} {selectedItem[0]}");
    }
}