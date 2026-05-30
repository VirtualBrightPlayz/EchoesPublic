using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class ControlPointManager : SingletonNode3D<ControlPointManager>
{
    public List<ControlPoint> Points = new List<ControlPoint>();

    public readonly Dictionary<TeamID, List<TeamID>> opposingForces = new Dictionary<TeamID, List<TeamID>>()
    {
        { TeamID.ClassD, new List<TeamID>() { TeamID.SCP, TeamID.NTF } },
        { TeamID.NTF, new List<TeamID>() { TeamID.SCP, TeamID.ClassD } },
    };

    public void CheckForEnd()
    {
        if (IsInstanceValid(RoundManager.Instance))
        {
            Dictionary<TeamID, int> captures = new Dictionary<TeamID, int>();
            foreach (var pt in Points)
            {
                if (!captures.ContainsKey(pt.owningTeam))
                    captures.Add(pt.owningTeam, 0);
                captures[pt.owningTeam]++;
            }

            KeyValuePair<TeamID, int> leadingTeam = captures.OrderByDescending(x => x.Value).FirstOrDefault();

            if (leadingTeam.Key != TeamID.Dead && leadingTeam.Value >= RoundManager.Instance.Logic.Config.RequiredControlPoints)
            {
                if (leadingTeam.Key == TeamID.ClassD)
                    RoundManager.Instance.EndRound("Insurgents win");
                else
                    RoundManager.Instance.EndRound("Foundation win");
            }
        }
    }

    public bool IsAbleToCapture(ZoneArea.Zone zone, TeamID team)
    {
        if (opposingForces.TryGetValue(team, out var teams))
        {
            foreach (var opposingTeam in teams)
            {
                if (IsAnyPlayerInZone(zone, opposingTeam))
                    return false;
            }
            return true;
        }
        return false;
    }

    public bool IsAnyPlayerInZone(ZoneArea.Zone zone, TeamID team)
    {
        foreach (ZoneArea area in GetTree().GetNodesInGroup(ZoneArea.GroupName))
        {
            if (area.zone == zone && area.controllersInZone.Any(x => x.Player.Role.team == team))
            {
                return true;
            }
        }
        return false;
    }
}