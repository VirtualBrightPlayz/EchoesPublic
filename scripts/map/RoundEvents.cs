using System;
using Godot;

[GlobalClass]
public partial class RoundEvents : Node
{
    [Export]
    public bool ServerOnly = false;

    [Signal]
    public delegate void OnRoundStartEventHandler();

    public override void _EnterTree()
    {
        RoundManager.Instance.EventOnRoundStart += RoundStart;
    }

    public override void _ExitTree()
    {
        RoundManager.Instance.EventOnRoundStart -= RoundStart;
    }

    private void RoundStart()
    {
        if (!ServerOnly || !Multiplayer.HasMultiplayerPeer() || Multiplayer.IsServer())
            EmitSignalOnRoundStart();
    }

}