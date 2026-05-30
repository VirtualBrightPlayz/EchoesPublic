using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Vent : Node3D, IInteractable
{
    public static List<Vent> All = new List<Vent>();

    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Touch;

    [Export]
    public Node3D marker;

    [Export]
    public string VentKey;

    [Export]
    public bool locked = false;

    [Export]
    public float VentTime = 0.5f;

    [Export]
    public Node3D EntryPoint;

    [Export]
    public Node3D ExitPoint;

    [Export]
    public RigidBody3D VentCover;

    [Export]
    public VentTeleporter Teleporter;

    [Export]
    public GameSound Sound;

    private NetworkPlayer player;
    private float timer;

    public override void _Ready()
    {
        All.Add(this);
    }

    public override void _ExitTree()
    {
        All.Remove(this);
    }

    /*
    public override void _PhysicsProcess(double delta)
    {
        if (player != null && !locked)
        {
            timer -= (float)delta;
            if (timer <= 0f)
            {
                if (Multiplayer.IsServer())
                {
                    var processedPlayer = player;
                    player = null;

                    if (VentCover.Freeze)
                    {
                        VentCover.Freeze = false;
                        VentCover.ApplyImpulse(VentCover.GlobalTransform.Basis.Y.Normalized() * -7.5f);
                    }

                    // processedPlayer.SV_Vent(this);
                }
            }
        }
    }
    */

    // [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SV_Vent(bool state)
    {
        Teleporter.Find();
        if (Teleporter.other == null) return;

        var multiplayerRemoteSenderId = Multiplayer.GetRemoteSenderId();
        if (state && (player == null || timer <= 0f))
        {
            // timer = VentTime;
            timer = 0f;
            player = IPlayerList.List(this).PlayerList.FirstOrDefault(x => multiplayerRemoteSenderId == x.GetMultiplayerAuthority());
            if (player != null && player.RoleIndex != (int)RoleID.SCP173)
            {
                player = null;
            }
        }
        else if (!state && player != null && multiplayerRemoteSenderId == player.GetMultiplayerAuthority())
        {
            player = null;
        }
    }

    public bool CanUse(IItemHolder holder, ItemObject item) => false;

    public void Use(IItemHolder holder, ItemObject item)
    {
        // if (Teleporter.other == null) return;

        // RpcId(1, nameof(SV_Vent), true);
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
        // if (Teleporter.other == null) return;

        // RpcId(1, nameof(SV_Vent), false);
    }
}