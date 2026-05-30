using System.Linq;
using Godot;

[GlobalClass]
public partial class ItemObject : Node
{
    public enum PickupFailReason : int
    {
        None = 0,
        InventoryFull,
        CantUse,
        ItemTypeLimitReached,
    }

    [Export]
    public int Serial = -1;
    [Export]
    public int Id = -1;
    public ItemPreset Preset;
    [Export]
    public NodePath playerPath
    {
        get => IsInstanceValid(Player) ? Player.GetPath() : default;
        set => Player = GetNodeOrNull<BasePlayer>(value);
    }
    public bool ModelEnabledCached;
    [Export]
    public bool ModelEnabled
    {
        get => IsInstanceValid(model) ? model.Visible : false;
        set
        {
            if (!IsInstanceValid(model))
                return;
            model.Visible = value;
        }
    }
    public bool ViewModelEnabledCached;
    [Export]
    public bool ViewModelEnabled
    {
        get => IsInstanceValid(viewModel) ? viewModel.Visible : false;
        set
        {
            if (!IsInstanceValid(viewModel))
                return;
            if (viewModel.Visible != value && value)
            {
                viewModel.Notification((int)Node.NotificationReady);
            }
            viewModel.Visible = value;
            viewModel.ProcessMode = value ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        }
    }
    public BasePlayer Player { get; protected set; }
    [Export]
    public Node3D model;
    [Export]
    public Node3D viewModel;
    [Export]
    public ItemGrip[] grips = new ItemGrip[0];
    [Export]
    public PhysicsBody3D[] physicsBodies = new PhysicsBody3D[0];

    public IElevatorTeleport elevatorTeleport => model as IElevatorTeleport;

    public IItemHolder PrimaryHolder => grips.FirstOrDefault(x => x.Holder != null)?.Holder;
    public IItemHolder[] Holders => grips.Select(x => x.Holder).Where(x => x != null).ToArray();

    public bool SenderIsPlayer => IsInstanceValid(Player) && Multiplayer.GetRemoteSenderId() == Player.GetMultiplayerAuthority();
    public bool SenderIsServer => Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority();

    public bool IsAuthorityOrServer => (!IsInstanceValid(Player) && IsMultiplayerAuthority()) || (IsInstanceValid(Player) && Player.IsMultiplayerAuthority());
    public bool IsAuthority => IsInstanceValid(Player) && Player.IsMultiplayerAuthority();

    public void Grab(bool visible)
    {
        Rpc(MethodName.RpcChangeOwner, visible);
    }

    public void Release(Vector3 pos, Vector3 rot, Vector3 vel = default, Vector3 ang = default)
    {
        Rpc(MethodName.RpcClearOwner, pos, rot, vel, ang);
    }

    public ItemGrip FindGrip(IItemHolder holder)
    {
        for (int i = 0; i < grips.Length; i++)
        {
            if (grips[i].Holder == holder)
            {
                return grips[i];
            }
        }
        return null;
    }

    public void GripGrab(IItemHolder holder)
    {
        grips[0].Hold(holder);
        for (int i = 1; i < grips.Length; i++)
        {
            grips[i].Hold(null);
        }
        UpdateModelStates();
        if (model is WorldItem item)
        {
            item.UpdateModelState();
        }
    }

    public bool GripGrabNext(IItemHolder holder)
    {
        if (Holders.Length == 0)
        {
            for (int i = 0; i < grips.Length; i++)
            {
                if (grips[i].Holder == null)
                {
                    grips[i].Hold(holder);
                    break;
                }
            }
            return true;
        }
        else
        {
            ItemGrip selected = null;
            float distance = float.MaxValue;
            for (int i = 0; i < grips.Length; i++)
            {
                if (grips[i].Holder == null)
                {
                    float dist = grips[i].marker.GlobalPosition.DistanceSquaredTo(holder.HolderTransform.Origin);
                    if (dist < distance)
                    {
                        distance = dist;
                        selected = grips[i];
                    }
                }
            }
            if (IsInstanceValid(selected))
            {
                selected.Hold(holder);
                return true;
            }
            else
                return false;
        }
    }

    public void GripsRelease()
    {
        foreach (var grip in grips)
        {
            grip.Hold(null);
        }
    }

    public void GripRelease(IItemHolder holder)
    {
        foreach (var grip in grips)
        {
            if (grip.Holder == holder)
                grip.Hold(null);
        }
    }

    public void SV_Teleport(Vector3 position, Vector3 rotation)
    {
        elevatorTeleport?.ElevatorTeleport(position, rotation);
    }

    public override void _EnterTree()
    {
        RequestReady();
    }

    public override void _Ready()
    {
        if (Serial == -1)
        {
            Log.PrintErr($"{GetPath()} is an invalid item!");
        }
        ItemManager.Instance.Items.Add(Serial, this);
        ModelEnabled = false;
        ViewModelEnabled = false;
        Preset = Id >= 0 && Id < ItemManager.Instance.Data.ItemPresets.Length ? ItemManager.Instance.Data.ItemPresets[Id] : null;
    }

    public override void _ExitTree()
    {
        ItemManager.Instance.Items.Remove(Serial);
    }

    public override void _Process(double delta)
    {
        UpdateModelStates();
    }

    public void UpdateModelStates()
    {
        ModelEnabledCached = ModelEnabled;
        ViewModelEnabledCached = ViewModelEnabled;
        // server code
        if (IsMultiplayerAuthority())
        {
            if (IsInstanceValid(Player))
            {
            }
            else
            {
                ModelEnabled = true;
            }
        }
        // claimant code
        if (IsInstanceValid(Player) && Player.IsMultiplayerAuthority())
        {
            bool tp = false;
            Transform3D xform;
            if (PrimaryHolder == null)
                xform = Player.AimTransform;
            else if (PrimaryHolder.IsSocket)
            {
                xform = PrimaryHolder.AimTransform;
                tp = true;
            }
            else if (PrimaryHolder == Player && Player.ActiveController != null && IsInstanceValid(Player.model) && IsInstanceValid(Player.model.rightHandBone))
            {
                var gripOffset = grips[0].marker.Transform.AffineInverse();
                xform = Player.model.rightHandBone.GlobalTransform * gripOffset;
                tp = true;
            }
            else
            {
                Node3D marker = grips[0].marker;
                if (PrimaryHolder is VRPhysicsHand hand)
                {
                    if (hand.isLeftHand)
                    {
                        if (IsInstanceValid(grips[0].leftMarker))
                            marker = grips[0].leftMarker;
                    }
                    else
                    {
                        if (IsInstanceValid(grips[0].rightMarker))
                            marker = grips[0].rightMarker;
                    }
                }
                var gripOffset = marker.Transform.AffineInverse();
                xform = PrimaryHolder.AimTransform * gripOffset;
            }
            ViewModelEnabled = PrimaryHolder == Player;
            if (tp)
            {
                model.GlobalPosition = xform.Origin;
                model.GlobalRotation = xform.Basis.GetEuler();
            }
            if (IsInstanceValid(viewModel))
            {
                viewModel.GlobalTransform = Player.AimTransform;
            }
            // kinda jank, but ok
            if (!Player.IsVR)
            {
                bool targetEnableState = PrimaryHolder == Player;
                if (ModelEnabledCached != targetEnableState)
                {
                    CL_SetModelState(targetEnableState);
                }
            }
        }
        // not claimant code
        else if (IsInstanceValid(Player))
        {
            Transform3D xform;
            bool tp = false;
            if (PrimaryHolder == null)
            {
                xform = Player.AimTransform;
            }
            else if (PrimaryHolder == Player && Player.ActiveController != null && IsInstanceValid(Player.model) && IsInstanceValid(Player.model.rightHandBone))
            {
                var gripOffset = grips[0].marker.Transform.AffineInverse();
                xform = Player.model.rightHandBone.GlobalTransform * gripOffset;
                tp = true;
            }
            else
                xform = PrimaryHolder.AimTransform;
            if (tp)
            {
                model.GlobalPosition = xform.Origin;
                model.GlobalRotation = xform.Basis.GetEuler();
            }
        }
        else
        {
            ViewModelEnabled = false;
        }
    }

    public PickupFailReason SV_Pickup(BasePlayer player, bool visible)
    {
        if (IsInstanceValid(player) && model is WorldItem item && !item.CanUse(player, null))
        {
            return PickupFailReason.CantUse;
        }
        if (IsInstanceValid(player) && model is WorldItem item2 && player.TryGetAbility(out InventoryAbility inventory2) && !inventory2.CanPickupType(item2.type))
        {
            return PickupFailReason.ItemTypeLimitReached;
        }
        if (IsInstanceValid(player) && (!player.TryGetAbility(out InventoryAbility inventory) || inventory.Inventory.Length >= inventory.MaxItems))
        {
            // foreach (var grip in grips)
            // {
                // grip.holderPath = null;
            // }
            // ModelEnabled = true;
            // SV_Teleport(player.PlayerPosition + Vector3.Up, player.PlayerRotation);
            return PickupFailReason.InventoryFull;
        }
        SV_SetPlayer(player, visible);
        return PickupFailReason.None;
    }

    /// sets the current player, removing anything grabbing in the process //, and setting the model visibility
    /// does not check inventory size.
    public void SV_SetPlayer(BasePlayer player, bool visible)
    {
        // TODO: interface maybe?
        if (IsInstanceValid(player) && player.TryGetAbility(out InventoryAbility inventory) && model is AmmoItem ammo)
        {
            foreach (var item in inventory.Inventory)
            {
                if (item.model is AmmoItem ammo1 && ammo.TypeOfAmmo == ammo1.TypeOfAmmo)
                {
                    ammo1.amount += ammo.amount;
                    QueueFree();
                    return;
                }
            }
        }
        foreach (var grip in grips)
        {
            grip.holderPath = null;
        }
        Player = player;
        ModelEnabled = visible;
        if (model is RigidbodySync sync)
        {
            // assume SendToAll sends the ClaimantId.
            if (IsInstanceValid(player))
            {
                sync.xformSync.ClaimantId = player.GetMultiplayerAuthority();
                sync.xformSync.relativeTo = player.ActiveControllerNode as Node3D;
            }
            else
            {
                sync.xformSync.ClaimantId = sync.xformSync.GetMultiplayerAuthority();
                sync.xformSync.relativeTo = null;
            }
            sync.xformSync.SendToAll();
            sync.UpdateFreezeState();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcChangeOwner(bool visible)
    {
        if (!IsMultiplayerAuthority() || IsInstanceValid(Player))
        {
            return;
        }
        // TODO: anti-cheat distance checks
        int remoteSender = Multiplayer.GetRemoteSenderId();
        var plr = IPlayerList.List(this).PlayerList.FirstOrDefault(x => remoteSender == x.GetMultiplayerAuthority());
        if (IsInstanceValid(plr))
        {
            PickupFailReason failReason = SV_Pickup(plr, visible);
            switch (failReason)
            {
                case PickupFailReason.InventoryFull:
                    // plr.RpcId(plr.GetMultiplayerAuthority(), BasePlayer.MethodName.CL_ClearHintQueue);
                    plr.RpcId(plr.GetMultiplayerAuthority(), BasePlayer.MethodName.CL_ShowHint, "HINT_INVENTORY_FULL", 1d);
                    break;
                case PickupFailReason.CantUse:
                    // plr.RpcId(plr.GetMultiplayerAuthority(), BasePlayer.MethodName.CL_ClearHintQueue);
                    plr.RpcId(plr.GetMultiplayerAuthority(), BasePlayer.MethodName.CL_ShowHint, "HINT_INVENTORY_CANT_USE", 1d);
                    break;
                case PickupFailReason.ItemTypeLimitReached:
                    // plr.RpcId(plr.GetMultiplayerAuthority(), BasePlayer.MethodName.CL_ClearHintQueue);
                    plr.RpcId(plr.GetMultiplayerAuthority(), BasePlayer.MethodName.CL_ShowHint, "HINT_INVENTORY_TYPE_LIMIT_REACHED", 1d);
                    break;
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public async void RpcClearOwner(Vector3 pos, Vector3 rot, Vector3 vel, Vector3 ang)
    {
        if (IsMultiplayerAuthority() && IsInstanceValid(Player) && SenderIsPlayer)
        {
            SV_Pickup(null, true);
            // GD.PrintS(pos, rot);
            SV_Teleport(pos, rot);
            if (model is RigidbodySync rb)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                rb.GlobalPosition = pos;
                rb.GlobalRotation = rot;
                rb.Freeze = false;
                rb.LinearVelocity = vel;
                rb.AngularVelocity = ang;
                rb.CallDeferred(RigidBody3D.MethodName.SetLinearVelocity, vel);
                rb.CallDeferred(RigidBody3D.MethodName.SetAngularVelocity, ang);
                rb.xformSync.SendToAll();
            }
        }
    }

    public void CL_SetModelState(bool state)
    {
        if (state)
            Rpc(MethodName.RpcCreateModel);
        else
            Rpc(MethodName.RpcDestroyModel);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcCreateModel()
    {
        if (SenderIsPlayer && IsMultiplayerAuthority())
        {
            ModelEnabled = true;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcDestroyModel()
    {
        if (SenderIsPlayer && IsMultiplayerAuthority())
        {
            ModelEnabled = false;
        }
    }
}