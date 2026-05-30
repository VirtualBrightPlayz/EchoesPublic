using System.Linq;
using Godot;

[GlobalClass]
public partial class InventoryAbility : BaseAbility
{
    // [Export]
    // public int[] AmmoAmounts { get; set; } = new int[3];
    [Export]
    public Godot.Collections.Dictionary<ItemType, int> Slots = new Godot.Collections.Dictionary<ItemType, int>()
    {
        { ItemType.Generic, 6 },
        { ItemType.Weapon, 1 },
        { ItemType.Keycard, 2 },
    };
    [Export]
    public int MaxItems = 8;

    public ItemObject[] Inventory => ItemManager.Instance.Items.Where(x => x.Value.Player == Player).Select(x => x.Value).ToArray();
    public ItemObject[] InventoryEquipped => ItemManager.Instance.Items.Where(x => x.Value.Player == Player && x.Value.PrimaryHolder != null).Select(x => x.Value).ToArray();

    public ButtonInputFlags buttonThrowItem = ButtonInputFlags.None;
    public ButtonInputFlags buttonInventory = ButtonInputFlags.None;

    public override void SetupFromDefinition()
    {
        int max = 0;
        if (TomlExtensions.TryGetValue(AbilityDef.dataCfg, "max_generic_items", out max))
        {
            Slots[ItemType.Generic] = max;
        }
        if (TomlExtensions.TryGetValue(AbilityDef.dataCfg, "max_weapon_items", out max))
        {
            Slots[ItemType.Weapon] = max;
        }
        if (TomlExtensions.TryGetValue(AbilityDef.dataCfg, "max_keycard_items", out max))
        {
            Slots[ItemType.Keycard] = max;
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        SetMultiplayerAuthority((int)MultiplayerPeer.TargetPeerServer);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (IsMultiplayerAuthority())
        {
            SV_DropAll();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!Player.IsLocalPlayer)
        {
            return;
        }
        UpdateInputs();
        if (buttonThrowItem.HasFlag(ButtonInputFlags.JustPressed))
        {
            foreach (var item in InventoryEquipped)
            {
                item.Release(Player.AimTransform.Origin, Player.AimTransform.Basis.GetEuler(), -Player.ActiveController.View.GlobalBasis.Z.Normalized() * 10f);
            }
        }
        if (Player is NetworkPlayer plr)
        {
            if (!InputManager.Instance.IsVR && buttonInventory.HasFlag(ButtonInputFlags.JustPressed))
            {
                plr.Hud.ToggleInventoryGui();
            }
            else if (InputManager.Instance.IsVR && buttonInventory.HasFlag(ButtonInputFlags.JustPressed))
            {
                plr.HudVR.ToggleInventoryGui();
            }
        }
    }

    public override void OnSpawn(PlayerRole role)
    {
        base.OnSpawn(role);
        if (IsMultiplayerAuthority())
        {
            SV_DropAll();
            for (int i = 0; i < role.StartItemNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(role.StartItemNames[i]))
                    continue;
                var item = Player.Data.ItemPresets.FirstOrDefault(x => x.ResourceName == role.StartItemNames[i]);
                if (IsInstanceValid(item))
                {
                    item.SpawnNew().SV_Pickup(Player, false);
                }
            }
            // AmmoAmounts = role.StartAmmo.ToArray();
            for (int i = 0; i < role.StartAmmo.Length; i++)
            {
                if (role.StartAmmo[i] <= 0)
                    continue;
                var preset = Player.Data.ItemPresets.FirstOrDefault(x => x is AmmoItemPreset p && (int)p.TypeOfAmmo == i);
                var item = preset.SpawnNew();
                (item.model as AmmoItem).amount = role.StartAmmo[i];
                item.SV_Teleport(Player.PlayerPosition + Vector3.Up, Player.PlayerRotation);
                item.SV_Pickup(Player, false);
            }
        }
    }

    public override void OnKilled()
    {
        base.OnKilled();
        if (IsMultiplayerAuthority())
        {
            SV_DropAll();
        }
    }

    private void UpdateInputs()
    {
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerThrow, ref buttonThrowItem);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerInventory, ref buttonInventory);
    }

    // TODO: Add helper functions to remove by serial, type or preset.
    
    public void SV_DropAll()
    {
        if (!Multiplayer.HasMultiplayerPeer())
            return;
        if (!Multiplayer.IsServer())
            return;
        var list = Inventory;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == null)
                continue;
            list[i].SV_Teleport(Player.PlayerPosition + Vector3.Up, Player.PlayerRotation);
            list[i].SV_SetPlayer(null, true);
        }
        // for (int i = 0; i < AmmoAmounts.Length; i++)
        {
            // AmmoAmounts[i] = 0;
        }
        /*
        for (int i = 0; i < AmmoAmounts.Length; i++)
        {
            if (AmmoAmounts[i] <= 0)
                continue;
            var item = Player.Data.AmmoItems[i].Instantiate<AmmoItem>();
            ItemManager.Instance.SpawnNode.AddChild(item, true);
            item.GlobalPosition = Player.PlayerPosition + Vector3.Up;
            item.GlobalRotation = Player.PlayerRotation;
            item.type = (AmmoType)i;
            item.amount = AmmoAmounts[i];
            AmmoAmounts[i] = 0;
        }
        */
    }

    public bool CanPickupType(ItemType type)
    {
        if (!Slots.TryGetValue(type, out int count))
        {
            return false;
        }
        ItemObject[] equipped = Inventory;
        for (int i = 0; i < equipped.Length; i++)
        {
            if (equipped[i].model is WorldItem item && item.type == type)
            {
                count--;
            }
        }
        // Log.Print("Count=", count);
        // Log.Print("Count2=", equipped.Length);
        return count > 0;
    }

    public AmmoItem GetAmmoItem(AmmoType type)
    {
        foreach (var item in Inventory)
        {
            if (item.model is AmmoItem ammo && ammo.TypeOfAmmo == type)
            {
                return ammo;
            }
        }
        return null;
    }
}
