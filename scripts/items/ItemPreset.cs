using System;
using Godot;

[GlobalClass]
public partial class ItemPreset : Resource
{
	[Export]
	public PackedScene ItemScene;
	[Export]
	public Texture2D Icon;
	[Export(PropertyHint.MultilineText)]
	public string Description = string.Empty;

	public virtual int Id => Array.IndexOf(ItemManager.Instance.Data.ItemPresets, this);

	public virtual ItemObject SpawnNew()
	{
		ItemObject item = ItemScene.Instantiate<ItemObject>();
		item.Id = Id;
		item.Serial = ItemManager.Instance.nextSerial;
		ItemManager.Instance.nextSerial++;
		item.Name += item.Serial.ToString();
		ItemManager.Instance.SpawnNode.AddChild(item, true);
		return item;
	}
}

public enum ItemType : int
{
	Generic = 1,
	Weapon = 2,
	Keycard = 4,
}
