using System;
using System.Linq;
using Godot;

[GlobalClass]
public partial class AmmoStash : StaticBody3D, IInteractable
{
    [Export] public int AmmoAmount = 100;
    [Export] public AmmoType AmmoType = AmmoType.AmmoPistol;
    [Export] public bool AllAmmoTypes = false;

    public Vector3 WorldInteractPosition => GlobalPosition;

    public IInteractable.InteractType ActionType => IInteractable.InteractType.Grab;

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return holder.GetPlayer().Role.team != TeamID.SCP;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        Rpc(MethodName.RpcGrabAmmo);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcGrabAmmo()
    {
        // TODO: re-add?
        /*
        var player = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AuthorityId == Multiplayer.GetRemoteSenderId());
        if (player.TryGetAbility(out InventoryAbility inventoryAbility))
        {
            if (AllAmmoTypes)
            {
                foreach (AmmoType type in Enum.GetValues<AmmoType>())
                {
                    int amount2 = inventoryAbility.AmmoAmounts[(int)type];
                    if (amount2 >= AmmoAmount)
                        continue;
                    inventoryAbility.AmmoAmounts[(int)type] = AmmoAmount;       
                }
                return;
            }
            int amount = inventoryAbility.AmmoAmounts[(int)AmmoType];
            if (amount >= AmmoAmount)
                return;
            inventoryAbility.AmmoAmounts[(int)AmmoType] = AmmoAmount;
        }
        */
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}