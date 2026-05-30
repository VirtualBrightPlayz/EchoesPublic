using System;
using System.Collections.Generic;
using System.Linq;
public class RemoveItemCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "RemoveItem";

    public override string[] Alias { get; } = [];

    public override string CommandDescription { get; } = "Removes an Item type from the player's inventory.";

    public override string ButtonLabel => "Remove Item";
    
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

        int removed = 0;
        
        foreach (NetworkPlayer player in players)
        {
            if (!player.TryGetAbility(out InventoryAbility inventoryAbility))
            {
                continue;
            }

            ItemObject[] itemsToRemove = inventoryAbility.Inventory.Where(item => item.Preset == itemPreset).ToArray();
            
            foreach (ItemObject item in itemsToRemove)
            {
                item.SV_SetPlayer(null, false);
                removed++;
            }
        }
        
        response = $"Removed {removed} {itemPreset?.ResourceName} instances from {string.Join(", ", players.Select(p => p.username))}.";
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
            admin.RunWithSelectedPlayers($"removeitem {{0}}");
        }
        
        admin.RunWithSelectedPlayers($"removeitem {{0}} {selectedItem[0]}");
    }
}