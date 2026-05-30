using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class VentTeleporter : Area3D
{
    public static List<VentTeleporter> All = new List<VentTeleporter>();

    [Export]
    public VentTeleporter other;

    [Export]
    public VentType Type;

    [Export]
    public bool Connector;

    /// <summary>
    /// In the direction that a player should be going
    /// when travelling through the portal.
    /// The base of this vector is this node's position.
    /// </summary>
    [Export]
    public Node3D Forward;

    public override void _Ready()
    {
        All.Add(this);
        BodyExited += Exit;
    }

    public override void _ExitTree()
    {
        All.Remove(this);
        BodyExited -= Exit;
    }

    public void Find()
    {
        if (other == null)
        {
            var arr = All.Where(x => x.Type == Type && x.Connector != Connector && x != this && x.other == null).ToArray();
            if (arr.Length == 0)
                return;
            other = arr[GD.Randi() % arr.Length];

            if (other != null)
            {
                other.other = this;
            }
        }
    }

    /// <summary>
    /// Is a player/position through the portal?
    /// </summary>
    public bool ThroughPortal(Vector3 position)
    {
        return (position - GlobalPosition).Normalized().Dot((Forward.GlobalPosition - GlobalPosition).Normalized()) > 0f;
    }

    private void Exit(Node3D body)
    {
        if (!IsMultiplayerAuthority())
            return;
        Find();
        if (other == null)
            return;

        if (body is IPlayerController plr && plr.Player.RoleIndex == (int)RoleID.SCP173)
        {
            // Only teleport them if they went through the portal.
            // This prevents them from going in and stepping backwards.
            if (!ThroughPortal(plr.Player.PlayerPosition)) return;

            var outPos = GlobalTransform.Inverse().TranslatedLocal(plr.Player.PlayerPosition).Origin;
            var outRot = GlobalRotation - plr.Player.PlayerRotation;
            outPos = other.GlobalTransform.TranslatedLocal(outPos).Origin;
            outRot = other.GlobalRotation - outRot;

            plr.Player.Rpc(nameof(NetworkPlayer.Teleport), outPos, outRot);
        }
    }

    public enum VentType
    {
        HorizontalRight,
        HorizontalLeft,
        VerticalUpRight,
        VerticalUpLeft,
        VerticalDownRight,
        VerticalDownLeft
    }
}