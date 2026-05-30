using System;
using Godot;

public partial class EscapeArea : Area3D
{
    public enum EscapeKind
    {
        Any,
        NTF,
        Chaos,
    }

    [Export]
    public EscapeKind kind = EscapeKind.Any;

    public override void _Ready()
    {
        BodyEntered += Enter;
    }

    public override void _ExitTree()
    {
        BodyEntered -= Enter;
    }

    private void Enter(Node3D body)
    {
        if (!IsMultiplayerAuthority())
            return;
        if (body is StaticBody3D)
            return;
        if (body is IPlayerController plr)
        {
            if (plr.Player.RoleIndex == (int)RoleID.Scientist)
            {
                switch (kind)
                {
                    case EscapeKind.Any:
                        plr.Player.SV_Spawn(RoleID.NTF);
                        break;
                    case EscapeKind.NTF:
                        plr.Player.SV_Spawn(RoleID.NTF);
                        break;
                    case EscapeKind.Chaos:
                        plr.Player.SV_Spawn(RoundManager.Instance.Logic.Config.NoCi ? RoleID.NTF : RoleID.Chaos);
                        break;
                }
            }
            else if (plr.Player.RoleIndex == (int)RoleID.ClassD)
            {
                switch (kind)
                {
                    case EscapeKind.Any:
                        plr.Player.SV_Spawn(RoundManager.Instance.Logic.Config.NoCi ? RoleID.NTF : RoleID.Chaos);
                        break;
                    case EscapeKind.NTF:
                        plr.Player.SV_Spawn(RoleID.NTF);
                        break;
                    case EscapeKind.Chaos:
                        plr.Player.SV_Spawn(RoundManager.Instance.Logic.Config.NoCi ? RoleID.NTF : RoleID.Chaos);
                        break;
                }
            }
        }
    }
}
