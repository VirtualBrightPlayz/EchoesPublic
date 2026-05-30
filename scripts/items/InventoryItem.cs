using System;
using System.IO;
using Godot;

[GlobalClass]
[Obsolete]
public partial class InventoryItem : Resource
{
    [Export(PropertyHint.MultilineText)]
    public string DisplayName;
    [Export]
    public Texture2D Icon;
    [Export]
    public PackedScene WorldItemScene;
    [Export]
    public PackedScene ViewItemScene;

    public int id = -1;
    public int serial = -1;
    protected int meta;

    public int Meta
    {
        get => meta;
        set
        {
            meta = value;
            UpdateItem();
        }
    }

    public virtual void UpdateItem()
    {
        // ItemManager.Instance.UpdateItem(this);
    }

    public virtual InventoryItem NewCopy()
    {
        return null;
        // return ItemManager.Instance.NewItem(this);
    }

    public InventoryItem Copy()
    {
        if (id == -1)
        {
            // id = Array.IndexOf(ItemManager.Instance.Data.Items, this);
        }
        InventoryItem clone = (InventoryItem)Duplicate();
        clone.id = id;
        clone.meta = meta;
        return clone;
    }

    public override string ToString()
    {
        return $"{DisplayName ?? ResourceName}";
    }

    public virtual WorldItem ToWorldItem()
    {
        WorldItem item = WorldItemScene.Instantiate<WorldItem>();
        // item.Item = this;
        item.Name = serial.ToString();
        ItemManager.Instance.SpawnNode.AddChild(item, true);
        return item;
    }

    public virtual WorldItem DropItem(IItemHolder holder)
    {
        return ToWorldItem();
    }

    public virtual WorldItem ThrowItem(IItemHolder holder, Vector3 origin, Vector3 dir)
    {
        holder.GetPlayer().Role.ApplyThrowSkills(origin, ref dir);
        var item = DropItem(holder);
        item.GlobalPosition = origin;
        item.ApplyCentralImpulse(dir);
        return item;
    }

    public byte[] ToBytes()
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(ms);
        WriteBytes(writer);
        return ms.ToArray();
    }

    public void FromBytes(byte[] arr)
    {
        using MemoryStream ms = new MemoryStream(arr);
        using BinaryReader reader = new BinaryReader(ms);
        ReadBytes(reader);
    }

    public void OnItemRpc(byte[] arr)
    {
        using MemoryStream ms = new MemoryStream(arr);
        using BinaryReader reader = new BinaryReader(ms);
        ReadItemRpc(reader);
    }

    public virtual void WriteBytes(BinaryWriter writer)
    {
        writer.Write(Meta);
    }

    public virtual void ReadBytes(BinaryReader reader)
    {
        meta = reader.ReadInt32();
    }

    public virtual void ReadItemRpc(BinaryReader reader)
    {
    }

#region Lifecycle Events

    public virtual void EnterTree(Node node)
    {
    }

    public virtual void ExitTree(Node node)
    {
    }

    public virtual void ProcessTree(Node node, double delta)
    {
    }

    public virtual void PhysicsProcessTree(Node node, double delta)
    {
    }

    public virtual void OnUse(Node node, IInteractable interactable)
    {
    }

#endregion

}
