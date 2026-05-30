using System;
using System.Linq;
using Godot;

[GlobalClass]
public partial class ControlPointLabel : Label3D
{
    [Export]
    public ZoneArea.Zone zone;

    public static string TranslateZone(ZoneArea.Zone zone)
    {
        switch (zone)
        {
            default:
            case ZoneArea.Zone.Unknown:
                return "Unknown";
            case ZoneArea.Zone.LightContainment:
                return "Light Containment";
            case ZoneArea.Zone.HeavyContainment:
                return "Heavy Containment";
            case ZoneArea.Zone.Entrance:
                return "Entrance";
            case ZoneArea.Zone.Surface:
                return "Surface";
        }
    }

    public static string TranslateTeam(TeamID team)
    {
        switch (team)
        {
            default:
                return string.Empty;
            case TeamID.NTF:
                return MainMenuUI.TranslateText("ROLE_NAME_NTF");
            case TeamID.ClassD:
                return MainMenuUI.TranslateText("ROLE_NAME_CI");
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (IsInstanceValid(ControlPointManager.Instance) && IsInstanceValid(NetworkPlayer.LocalInstance))
        {
            ControlPoint point = ControlPointManager.Instance.Points.FirstOrDefault(x => x.zone == zone);
            if (IsInstanceValid(point))
            {
                // NetworkPlayer.LocalInstance.Role.team
                switch (point.state)
                {
                    case ControlPoint.State.Idle:
                        if (point.owningTeam == TeamID.Dead && NetworkPlayer.LocalInstance.Role.CanCapturePoints)
                        {
                            Text = "Ready to Capture.";
                        }
                        else if (point.owningTeam == TeamID.Dead)
                        {
                            Text = "Not Captured.";
                        }
                        else
                        {
                            Text = $"Taken by {TranslateTeam(point.owningTeam)}.";
                        }
                        break;
                    case ControlPoint.State.Using:
                        Text = "Capture in progress...\nPlease hold.";
                        break;
                    case ControlPoint.State.Capturing:
                        if (point.capturingTeam == NetworkPlayer.LocalInstance.Role.team)
                        {
                            if (point.ableToCapture)
                                Text = "Defend this zone.";
                            else
                                Text = "This zone is being contested.\nFind and execute all enemies from this zone!";
                        }
                        else
                        {
                            Text = $"This zone is under {TranslateTeam(point.capturingTeam)} control!\nReady to Capture.";
                        }
                        break;
                }
            }
        }
    }
}
