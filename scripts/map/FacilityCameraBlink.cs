using Godot;

public partial class FacilityCameraBlink : Node
{
    [Export]
    public FacilityCamera camera;
    [Export]
    public Node3D target;
    [Export]
    public Node3D audio;

    public void StateCheck()
    {
        audio.Visible = camera.IsActivelyViewed;
        if (!camera.IsActivelyViewed)
        {
            target.Visible = false;
            return;
        }
        target.Visible = !target.Visible;
    }
}