using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CameraCommand : SimpleGameCommandBase
{
    public override string Command => "Cameras";

    public override string[] Alias => new string[] { "cams" };

    public override string CommandDescription => "Camera Debug Information";

    public override bool Execute(string[] args, out string response)
    {
        if (FacilityCameraManager.Instance == null)
        {
            response = "No FacilityCameraManager found!";
            return false;
        }
        response = $"Camera Status\nTotal Cameras: {FacilityCameraManager.Instance.GetCameras().Count}\nCameras:\n";
        foreach (FacilityCamera camera in FacilityCameraManager.Instance.GetCameras())
        {
            response += $"ID: {camera.id}, OwnerID: {camera.ownerId}, Position: {camera.GlobalPosition}, Rotation: {camera.GlobalRotation}, Zone: {camera.ZoneArea}\n";
        }
        return true;
    }
}