using Godot;

[GlobalClass]
public partial class GrenadeBase : WorldItem
{
	public enum GrenadeState : int
	{
		Idle = 0,
		Throwing = 1,
		Ignited = 2,
		Invalid = 3,
	}

	[Export]
	public PackedScene GrenadeScene;
	[Export]
	public float throwTime = 1f;
	[Export]
	public float ExplosionTimer = 2f;
	[Export]
	public GameSound pinSound;
	[Export]
	public MeshInstance3D pin;
	[Export]
	public bool CanFire = true;

	public GrenadeState Meta = GrenadeState.Idle;
	public float Timer = 0f;
	public bool underhand = false;
	
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		IItemHolder holder = Item.PrimaryHolder;
		if (IsMultiplayerAuthority() && Meta == GrenadeState.Ignited)
		{
			Timer -= (float)delta;
			if (holder == null)
			{
				var grenade = Spawn();
				grenade.GlobalPosition = GlobalPosition;
				grenade.GlobalRotation = GlobalRotation;
				grenade.SetDefaultVelocity(LinearVelocity);
				grenade.ExplosionTimer = Timer;
				Item.QueueFree();
				Meta = GrenadeState.Invalid;
			}
			else if (Timer <= 0f)
			{
				var grenade = Spawn();
				if (holder != null)
				{
					Vector3 fwd = (holder.AimTransform.Basis * Vector3.Forward).Normalized();
					grenade.GlobalPosition = holder.AimTransform.Origin + fwd * 0.15f;
				}
				else
				{
					grenade.GlobalPosition = GlobalPosition;
					grenade.GlobalRotation = GlobalRotation;
				}
				grenade.ExplosionTimer = 0f;
				Item.QueueFree();
				Meta = GrenadeState.Invalid;
			}
		}

		if (Item.PrimaryHolder != null && Item.Player.HasAuthority && Meta != GrenadeState.Ignited)
		{
			BasePlayer player = Item.Player;
			if (Timer > 0f && Meta == GrenadeState.Throwing)
			{
				Timer -= (float)delta;
			}
			else if (Meta == GrenadeState.Throwing)
			{
				// Timer = ExplosionTimer;
				Meta = GrenadeState.Invalid;
				Rpc(MethodName.RpcThrowRequest, underhand);
			}

			if (Item.PrimaryHolder.InputPrimary.HasFlag(ButtonInputFlags.JustPressed) && Meta == GrenadeState.Idle && CanFire)
			{
				if (Item.PrimaryHolder is VRPhysicsHand)
				{
					return;
					// Timer = ExplosionTimer;
					Meta = GrenadeState.Ignited;
					Rpc(MethodName.RpcThrowEnd);
				}
				else
				{
					underhand = false;
					Timer = throwTime;
					Meta = GrenadeState.Throwing;
					Rpc(MethodName.RpcThrowStart);
				}
			}
			else if (Item.PrimaryHolder == Item.Player && Item.Player.IsAimingDown && Meta == GrenadeState.Idle && CanFire)
			{
				underhand = true;
				Timer = throwTime;
				Meta = GrenadeState.Throwing;
				Rpc(MethodName.RpcThrowStart);
			}
		}
	}

	public void GrabUpdated(ItemGrip grip, Node last)
	{
		if (last is IItemHolder holder)
		{
			if (Meta == GrenadeState.Ignited)
			{
				// ThrownGrenade grenade = Spawn();
				// grenade.GlobalPosition = GlobalPosition;
			}
		}
	}

	public void HandThrow(ItemGrip grip, Node lastHolder)
	{
		if (grip.Holder is VRPhysicsHand && Item.Player.HasAuthority && Meta != GrenadeState.Ignited)
		{
			Meta = GrenadeState.Ignited;
			Rpc(MethodName.RpcThrowEnd);
		}
		else if (lastHolder is VRPhysicsHand && Meta == GrenadeState.Ignited)
		{
			if (IsInstanceValid(pin))
				pin.Visible = false;
		}
	}

	public virtual ThrownGrenade Spawn()
	{
		var grenade = GrenadeScene.Instantiate<ThrownGrenade>();
		ItemManager.Instance.SpawnNode.AddChild(grenade, true);
		grenade.throwerPath = Item.playerPath;
		if (IsInstanceValid(Item.Player))
			grenade.throwerTeam = Item.Player.Role.team;
		return grenade;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void RpcThrowStart()
	{
		if (Item.SenderIsPlayer)
		{
			if (Item.viewModel is ViewmodelGrenade view)
			{
				view.ViewmodelEventThrowStart();
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void RpcThrowRequest(bool underhand)
	{
		if (IsMultiplayerAuthority() && Item.SenderIsPlayer)
		{
			Throw(Item.Player, underhand);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void RpcThrowEnd()
	{
		if (Item.SenderIsPlayer)
		{
			pinSound.PlayOneShot3D(this);
		}
		if (IsMultiplayerAuthority() && Item.SenderIsPlayer)
		{
			Timer = ExplosionTimer;
			Meta = GrenadeState.Ignited; // ignite
		}
	}

	public virtual void Throw(IItemHolder holder, bool underhand)
	{
		Vector3 fwd = (holder.AimTransform.Basis * Vector3.Forward).Normalized();
		Vector3 basePostion = holder.AimTransform.Origin + (fwd * 0.15f);
		Vector3 basePostionSticky = holder.AimTransform.Origin + (fwd * 0.3f);

		BasePlayer player = holder.GetPlayer();
		player.Role.ApplyThrowSkills(basePostion, ref fwd);

		var grenade = Spawn();
		grenade.GlobalPosition = basePostion;
		// grenade.throwerPath = player.GetPath();
		if (!underhand)
			grenade.SetDefaultVelocity(fwd);

		// Item.SV_SetPlayer(null, true);
		Item.QueueFree();
		Meta = GrenadeState.Invalid;
		// player.InventorySerials[player.EquippedItemIndex] = -1;
		// player.EquippedItemIndex = -1;
		// ItemManager.Instance.DeleteItem(this);
	}
}
