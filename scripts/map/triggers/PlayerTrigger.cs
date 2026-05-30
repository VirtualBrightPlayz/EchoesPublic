using Godot;
using System;

[GlobalClass]
public partial class PlayerTrigger : Area3D
{
    [Signal]
    public delegate void PlayerEnterEventHandler(NetworkPlayer player);

    [Export] public bool triggerOnce = false;
    [ExportGroup("Team Filter")]
    [Export(PropertyHint.GroupEnable)] public bool teamFilterEnabled = false;
    [Export] public TeamID teamFilter = TeamID.Dead;

    private bool triggered = false;

    public override void _EnterTree()
    {
        BodyEntered += _Entered;
        CollisionMask = 2;
        RoundManager.Instance.EventOnRoundStart += Reset;
    }

    private void Reset()
    {
        triggered = false;
    }

    public override void _ExitTree()
    {
        BodyEntered -= _Entered;
        RoundManager.Instance.EventOnRoundStart -= Reset;
    }

    private void _Entered(Node3D body)
    {
        if (!IsMultiplayerAuthority())
            return;
        if (triggered && triggerOnce)
            return;
        if (body is IPlayerController ctrl && ctrl.Player is NetworkPlayer player)
        {
            if (!teamFilterEnabled || (player.Role.team == teamFilter))
            {
                EmitSignalPlayerEnter(player);
                triggered = true;
            }
        }
    }
}
