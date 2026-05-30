using Godot;
using System.Collections.Generic;
using System.Collections.ObjectModel;

[GlobalClass]
public partial class FacilityCameraManager : SingletonNode3D<FacilityCameraManager>
{
    List<FacilityCamera> Cameras = new List<FacilityCamera>();

    int _curCamId = 0;

    /// <summary>
    /// Registers a camera and assigns it an ID.
    /// </summary>
    /// <param name="camera"></param>
    public void Register(FacilityCamera camera)
    {
        if(camera.Multiplayer.IsServer())
        {
            if(Cameras.Contains(camera))
            {
                return;
            }
            camera.id = _curCamId;
            _curCamId++;
        }
        Cameras.Add(camera);
    }

    /// <summary>
    /// Removes a camera, note that the ID is not freed, thus gaps can exist
    /// </summary>
    /// <param name="camera"></param>
    public void Unregister(FacilityCamera camera)
    {
        if (Cameras.Contains(camera))
        {
            Cameras.Remove(camera);
            // camera.QueueFree();
        }
    }

    /// <summary>
    /// Gets a camera by ID, if not found, returns the <see cref="default"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public FacilityCamera GetCamera(int id)
    {
        return Cameras.Find(x => x.id == id);
    }

    /// <summary>
    /// Gets a copy of the Camera list
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<FacilityCamera> GetCameras()
    {
        return new ReadOnlyCollection<FacilityCamera>(Cameras);
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        _curCamId = 0;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        //When we exit, kill everyone else.
        foreach(FacilityCamera camera in Cameras)
        {
            if (IsInstanceValid(camera))
                camera.QueueFree();
        }
    }
}
