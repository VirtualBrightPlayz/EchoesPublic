using System.Linq;
using Godot;

[GlobalClass]
public partial class ReviveAbility : BaseAbility
{
    [Export]
    public PackedScene reviveNpcPrefab;
    [Export]
    public bool forceNpc = false;

    public override void SetupFromDefinition()
    {
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcRevive(NodePath peerId)
    {
        if (!Multiplayer.IsServer())
            return;
        Node node = GetNodeOrNull(peerId);
        if (IsInstanceValid(node) && node is Ragdoll ragdoll && ragdoll.Role.team != TeamID.SCP)
        {
            var victims = IPlayerList.List(this).PlayerList.Where(x => x.Role.team == TeamID.Dead).ToArray();
            ragdoll.GetParent().QueueFree();
            if (victims.Length == 0 || forceNpc)
            {
                if (IsInstanceValid(reviveNpcPrefab))
                {
                    var npc = reviveNpcPrefab.Instantiate<Node3D>();
                    ItemManager.Instance.SpawnNode.AddChild(npc, true);
                    npc.GlobalPosition = ragdoll.skeleton.GlobalPosition;
                }
                return;
            }
            var victim = victims[(int)(GD.Randi() % victims.Length)];
            victim.SV_Spawn(RoleID.SCP049_2, ragdoll.skeleton.GlobalPosition);
        }
    }
}
