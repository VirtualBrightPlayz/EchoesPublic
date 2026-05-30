using Godot;
using System;

public partial class PlayerSpawnpoint : Marker3D
{
    [Export]
    public PlayerRole role;
    [Export]
    public TeamID team = TeamID.Dead;

    public int GetSpawnPriority(PlayerRole playerRole)
    {
        if (!IsVisibleInTree())
            return 0;
        if (IsInstanceValid(role) && (role == playerRole || role.ResourceName == playerRole.ResourceName))
            return 2;
        return playerRole.team == team ? 1 : 0;
    }

    public bool CanSpawn(PlayerRole playerRole)
    {
        if (!IsVisibleInTree())
            return false;
        if (IsInstanceValid(role))
            return role == playerRole || role.ResourceName == playerRole.ResourceName;
        return false;
        // return playerRole.team == team;
    }

    public bool CanSpawn(TeamID teamId)
    {
        if (!IsVisibleInTree())
            return false;
        if (IsInstanceValid(role))
            return role.team == teamId;
        return teamId == team;
    }
}
