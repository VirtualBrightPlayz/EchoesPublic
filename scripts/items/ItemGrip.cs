using System.Linq;
using Godot;

[GlobalClass]
public partial class ItemGrip : Node
{
    [Signal]
    public delegate void OnHeldEventHandler(ItemGrip grip, Node lastHolder);
    [Signal]
    public delegate void OnUnHeldEventHandler(ItemGrip grip, Node lastHolder);

    [Export]
    public ItemObject item;
    [Export]
    public RigidBody3D target;
    [Export]
    public Node3D marker;
    [Export]
    public Node3D leftMarker;
    [Export]
    public Node3D rightMarker;

    private NodePath _holderPath;
    [Export]
    public NodePath holderPath
    {
        get => _holderPath;
        set
        {
            if (_holderPath != value)
            {
                NodePath p = _holderPath;
                _holderPath = value;
                if (value != null && !value.IsEmpty)
                {
                    Holder = GetNodeOrNull<IItemHolder>(value);
                }
                else
                {
                    Holder = null;
                }
                if (value == null || value.IsEmpty)
                    EmitSignalOnUnHeld(this, GetNodeOrNull(p));
                else
                    EmitSignalOnHeld(this, GetNodeOrNull(p));
            }
        }
    }

    public IItemHolder Holder { get; protected set; }
    /*
    {
        get => GetNodeOrNull<IItemHolder>(holderPath);
        set => holderPath = IsInstanceValid((GodotObject)value) ? value.GetPath() : null;
    }
    */

    public bool SenderIsHolder => Multiplayer.GetRemoteSenderId() == Holder.GetPlayer().GetMultiplayerAuthority();
    public bool SenderIsServer => Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority();

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(target) && Holder == null)
        {
            target.Position = Vector3.Zero;
            target.Rotation = Vector3.Zero;
            target.LinearVelocity = Vector3.Zero;
            target.AngularVelocity = Vector3.Zero;
            // target.Freeze = true;
        }
    }

    public void Hold(IItemHolder holder)
    {
        Rpc(MethodName.RpcHoldState, holder?.GetPath());
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcHoldState(NodePath path)
    {
        if (item.SenderIsPlayer /*&& IsMultiplayerAuthority()*/)
        {
            IItemHolder last = Holder;
            holderPath = path;
            // EmitSignalOnHeld(this, last as Node);
        }
    }
}