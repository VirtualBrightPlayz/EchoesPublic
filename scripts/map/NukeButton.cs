using System.Linq;
using Godot;

public partial class NukeButton : StaticBody3D, IInteractable
{
    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Grab;

    [Export]
    public Node3D marker;

    [Export]
    public bool isCancel = false;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse()
    {
        // TODO: anti-cheat checks
        if (Multiplayer.IsServer() && !RoundManager.Instance.nukeDetonated)
        {
            var sender = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AuthorityId == Multiplayer.GetRemoteSenderId());
            if (IsInstanceValid(sender) && sender.PlayerPosition.DistanceSquaredTo(GlobalPosition) <= 10f && (isCancel || sender.Role.team != TeamID.SCP))
            {
                if (isCancel)
                {
                    if (RoundManager.Instance.nukeActive)
                        RoundManager.Instance.StopNuke();
                }
                else
                {
                    if (!RoundManager.Instance.nukeActive)
                        RoundManager.Instance.StartNuke();
                }
            }
        }
    }

    public bool CanUse(IItemHolder holder, ItemObject item) => isCancel || holder.GetPlayer().Role.team != TeamID.SCP;

    public void Use(IItemHolder holder, ItemObject item)
    {
        RpcId(1, nameof(RpcUse));
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}
