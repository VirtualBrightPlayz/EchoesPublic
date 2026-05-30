using Godot;

public interface IGrabbable
{
    void Grab(bool grab, Vector3 localPos = default, Vector3 releaseForce = default);
    
    public float Mass { get; }
    
    public bool CurrentlyGrabbed { get; }
    
    [Export]
    public bool AllowGrab { get; set; }
    
    public Node Sync { get; }
}