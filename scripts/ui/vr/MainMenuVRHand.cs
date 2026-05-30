using Godot;

public partial class MainMenuVRHand : Node3D
{
    [Export]
    public XRController3D hand;
    [Export]
    public MainMenu menu;

    public Transform3D lastXform;

    private bool wasPressed = false;

    public override void _Process(double delta)
    {
        Visible = GetViewport().UseXR;
        if (!Visible)
            return;
        Transform3D xform = lastXform;
        float trigger = hand.GetFloat("trigger");
        if (trigger < 0.25f || trigger > 0.75f)
            xform = hand.GlobalTransform;
        GlobalTransform = xform;
        bool pressed = trigger > 0.5f;
        if (wasPressed != pressed)
            menu.HandleRaycastPress(pressed, xform.Origin, -xform.Basis.Z.Normalized());
        if (trigger >= 0.25f)
            menu.HandleRaycastMotion(pressed, xform.Origin, -xform.Basis.Z.Normalized(), lastXform.Origin, -lastXform.Basis.Z.Normalized());
        wasPressed = pressed;
        lastXform = xform;
    }
}