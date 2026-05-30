using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class InventoryUI : Node
{
    public NetworkPlayer Player => NetworkPlayer.LocalInstance;

    [Export]
    public Control root;
    [Export]
    public Control container;
    // [Export]
    // public DropButton template;
    [Export]
    public RichTextLabel inventoryText;
    [Export]
    public Control tooltipContainer;
    public DropButton[] inventorySlots;
    [Export]
    public BaseButton buttonOne;
    [Export]
    public BaseButton buttonTwo;

    private MouseButtonMask lastButtonMask;

    public override void _Ready()
    {
        base._Ready();
        root.VisibilityChanged += VisChanged;
        inventorySlots = container.FindChildren("*").OfType<DropButton>().ToArray();
        foreach (var tex in inventorySlots)
        {
            tex.MouseEntered += () => InvHovered(tex);
            tex.MouseExited += () => InvHovered(null);
            tex.lastSerial = -1;
        }
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        InputActionDigital input = InputManager.Instance.CurrentSet.DigitalActions.FirstOrDefault(x => x.ResourceName == LocalPlayerInput.InventoryOne);
        InputActionDigital input2 = InputManager.Instance.CurrentSet.DigitalActions.FirstOrDefault(x => x.ResourceName == LocalPlayerInput.InventoryTwo);
        if (IsInstanceValid(input) && ev.IsActionPressed(input.GodotAction))
        {
            UpdateTextures();
            InvSelected(buttonOne, true);
        }
        if (IsInstanceValid(input2) && ev.IsActionPressed(input2.GodotAction))
        {
            UpdateTextures();
            InvSelected(buttonTwo, true);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var hovered = GetViewport().GuiGetFocusOwner();
        if (root.IsVisibleInTree() && lastButtonMask.HasFlag(MouseButtonMask.Left) && !IsInstanceValid(hovered) && !GetViewport().GuiIsDragging())
        {
            InvSelected(null, false);
        }
        lastButtonMask = Input.GetMouseButtonMask();
        if (root.IsVisibleInTree() && !string.IsNullOrWhiteSpace(inventoryText.Text))
        {
            tooltipContainer.Visible = true;
            tooltipContainer.GlobalPosition = GetViewport().GetMousePosition() + new Vector2(0f, 32f + tooltipContainer.GetMinimumSize().Y);
        }
        else
        {
            tooltipContainer.Visible = false;
        }
    }

    public void InvSelected(Node node, bool force)
    {
        if (!root.IsVisibleInTree() && !force)
            return;
        if (!Player.TryGetAbility(out InventoryAbility inventory))
            return;
        var list = inventory.InventoryEquipped;
        foreach (var item in list)
        {
            item.GripsRelease();
        }
        if (node == null)
        {
            Player.Hud.CloseInventoryGui();
            return;
        }

        if (node is DropButton btn)
        {
            int j = Array.IndexOf(inventorySlots, node);
            if (lastButtonMask.HasFlag(MouseButtonMask.Left) || force)
            {
                if (IsInstanceValid(btn.itemObj))
                    btn.itemObj.GripGrab(Player);
                Player.Hud.CloseInventoryGui();
            }
            else if (lastButtonMask.HasFlag(MouseButtonMask.Right))
            {
                if (IsInstanceValid(btn.itemObj))
                    btn.itemObj.Release(Player.AimTransform.Origin, Player.AimTransform.Basis.GetEuler());
                UpdateTextures();
                InvHovered(null);
            }
        }
    }

    public void InvHovered(Node node)
    {
        if (!root.IsVisibleInTree())
            return;
        if (node == null)
        {
            inventoryText.Text = string.Empty;
            GetViewport().GuiReleaseFocus();
            return;
        }

        if (node is DropButton btn)
        {
            btn.GrabFocus();
            // int j = Array.IndexOf(inventorySlots, node);
            if (IsInstanceValid(btn.itemObj))
                inventoryText.Text = btn.itemObj.model?.ToString();
            else
                inventoryText.Text = string.Empty;
        }
    }

    public void VisChanged()
    {
        if (!root.IsVisibleInTree())
            return;
        InvHovered(null);
        UpdateTextures();
    }

    public void UpdateTextures()
    {
        bool hasInv = Player.TryGetAbility(out InventoryAbility inventoryAbility);
        List<ItemObject> inventory = hasInv ? inventoryAbility.Inventory.ToList() : [];
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            inventorySlots[i].SetItem(null);
        }
        // find existing items
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].lastSerial == -1)
            {
                continue;
            }
            inventorySlots[i].UpdateMinimumSize();
            ItemObject selected = null;
            foreach (var item in inventory)
            {
                if (inventorySlots[i].lastSerial == item.Serial)
                {
                    selected = item;
                    inventory.Remove(item);
                    break;
                }
            }
            inventorySlots[i].SetItem(selected);
            inventorySlots[i].lastSerial = selected?.Serial ?? -1;
        }
        // find new items
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (IsInstanceValid(inventorySlots[i].itemObj))
            {
                continue;
            }
            inventorySlots[i].UpdateMinimumSize();
            ItemObject selected = null;
            var item = inventory.FirstOrDefault();
            if (IsInstanceValid(item) && item.model is WorldItem worldItem && (worldItem.type == inventorySlots[i].type /*|| inventorySlots[i].type == ItemType.Generic*/))
            {
                selected = item;
                inventory.Remove(item);
            }
            inventorySlots[i].SetItem(selected);
            inventorySlots[i].lastSerial = selected?.Serial ?? -1;
        }
        // find new items part 2
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (IsInstanceValid(inventorySlots[i].itemObj))
            {
                continue;
            }
            inventorySlots[i].UpdateMinimumSize();
            ItemObject selected = null;
            var item = inventory.FirstOrDefault();
            if (IsInstanceValid(item) && item.model is WorldItem worldItem && inventorySlots[i].type == ItemType.Generic)
            {
                selected = item;
                inventory.Remove(item);
            }
            inventorySlots[i].SetItem(selected);
            inventorySlots[i].lastSerial = selected?.Serial ?? -1;
        }
    }
}