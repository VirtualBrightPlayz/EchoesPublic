using Godot;
using System;

[GlobalClass]
public partial class DropButton : TextureButton
{
    [Export]
    public LocalPlayerHUD hud;
    [Export]
    public InventoryUI invUI;
    [Export]
    public ItemObject itemObj;
    [Export]
    public ItemType type = ItemType.Generic;

    public int lastSerial = -1;

    public void SetItem(ItemObject obj)
    {
        itemObj = obj;
        TextureNormal = obj?.Preset?.Icon;
    }

    public override void _Pressed()
    {
        invUI.InvSelected(this, false);
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        Control ctrl = new Control();
        TextureRect rect = new TextureRect();
        rect.Texture = TextureNormal;
        rect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        rect.StretchMode = TextureRect.StretchModeEnum.Scale;
        rect.CustomMinimumSize = CustomMinimumSize;
        ctrl.AddChild(rect);
        rect.Size = Size;
        rect.SelfModulate = new Color(1f, 1f, 1f, 0.5f);
        if (atPosition.IsFinite())
        {
            rect.Position = -atPosition;
        }
        SetDragPreview(ctrl);
        return itemObj;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return data.VariantType == Variant.Type.Object && data.AsGodotObject() is ItemObject obj && obj.model is WorldItem item && (item.type == type || (type == ItemType.Generic && !IsInstanceValid(itemObj)));
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        int serial = data.As<ItemObject>().Serial;
        for (int i = 0; i < invUI.inventorySlots.Length; i++)
        {
            if (invUI.inventorySlots[i].lastSerial == serial)
            {
                invUI.inventorySlots[i].lastSerial = lastSerial;
            }
        }
        lastSerial = serial;
        invUI.UpdateTextures();
    }

}
