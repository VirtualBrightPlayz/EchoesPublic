using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class MapDoor : Marker3D
{
	[Signal]
	public delegate void OnDoorUsedEventHandler();

	[Export]
	public Array<Node3D> showOnUse = new Array<Node3D>();
	[Export]
	public bool ShouldSpawn = true;
	[Export]
	public bool RequireOther = false;

	public bool Used
	{
		get => GetMeta("used", false).AsBool();
		set => SetMeta("used", value);
	}

	public int DoorType
	{
		get => GetMeta("door_type", 0).AsInt32();
		set => SetMeta("door_type", value);
	}

	public bool Optional
	{
		get => GetMeta("optional", false).AsBool();
		set => SetMeta("optional", value);
	}

	public bool OtherUsed
	{
		get => GetMeta("other_used", false).AsBool();
		set => SetMeta("other_used", value);
	}
	
	public bool usedState = false;

	public override void _EnterTree()
	{
		base._EnterTree();
		// RoundManager.Instance.EventOnPlayerJoined += _PlayerJoined;
		Multiplayer.PeerConnected += _Joined;
		foreach (var item in showOnUse)
		{
			if (!IsInstanceValid(item))
				continue;
			if (item is Door door)
			{
				door.isLocked = true;
				continue;
			}
			item.Visible = false;
			if (item is CollisionShape3D shape)
			{
				shape.Disabled = true;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		// RoundManager.Instance.EventOnPlayerJoined -= _PlayerJoined;
		Multiplayer.PeerConnected -= _Joined;
	}

	private void _Joined(long id)
	{
		if (IsMultiplayerAuthority() && showOnUse.Count != 0)
		{
			RpcId(id, MethodName.RpcUse, usedState);
		}
	}

	private void _PlayerJoined(NetworkPlayer player)
	{
		if (IsMultiplayerAuthority() && showOnUse.Count != 0)
		{
			RpcId(player.GetMultiplayerAuthority(), MethodName.RpcUse, usedState);
		}
	}

	public void Use()
	{
		EmitSignalOnDoorUsed();
		if (RequireOther)
			usedState = Used && OtherUsed;
		else
			usedState = Used || OtherUsed;
		Rpc(MethodName.RpcUse, usedState);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void RpcUse(bool state)
	{
		foreach (var item in showOnUse)
		{
			if (!IsInstanceValid(item))
				continue;
			if (item is Door door)
			{
				door.isLocked = !state;
				continue;
			}
			item.Visible = state;
			if (item is CollisionShape3D shape)
			{
				shape.Disabled = !state;
			}
		}
	}
}
