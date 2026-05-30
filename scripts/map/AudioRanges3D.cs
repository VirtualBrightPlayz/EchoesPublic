using Godot;
using System;

[GlobalClass]
[Tool]
public partial class AudioRanges3D : AudioStreamPlayer3D
{
    [Export]
    public Node3D cam;

    [Export]
    public float closeMaxRange = 10f;
    [Export]
    public float mediumMaxRange = 20f;
    [Export]
    public float farMaxRange = 30f;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;
        if (IsInstanceValid(Stream))
        {
            Stream.ResourceLocalToScene = true;
        }
    }

	public override void _Process(double delta)
	{
        // if (Engine.IsEditorHint())
        //     return;
        if (!IsInstanceValid(cam))
        {
            cam = GetViewport().GetCamera3D();
            return;
        }
        float dist = GlobalPosition.DistanceTo(cam.GlobalPosition);
        if (Playing && Stream is AudioStreamSynchronized sync && sync.ResourceLocalToScene && sync.StreamCount > 0)
        {
            float nearVol = Mathf.InverseLerp(closeMaxRange, 0f, dist);
            float midVol = Mathf.InverseLerp(mediumMaxRange, closeMaxRange, dist);
            float farVol = Mathf.InverseLerp(farMaxRange, mediumMaxRange, dist);
            nearVol = 1f - Mathf.Abs(1f - nearVol);
            midVol = 1f - Mathf.Abs(1f - midVol);
            farVol = 1f - Mathf.Abs(1f - farVol);
            // GD.PrintS(nearVol, midVol, farVol);
            nearVol = Mathf.LinearToDb(Mathf.Clamp(nearVol, 0f, 1f));
            midVol = Mathf.LinearToDb(Mathf.Clamp(midVol, 0f, 1f));
            farVol = Mathf.LinearToDb(Mathf.Clamp(farVol, 0f, 1f));
            // GD.PrintS(nearVol, midVol, farVol);
            sync.SetSyncStreamVolume(0, nearVol);
            if (sync.StreamCount > 1)
                sync.SetSyncStreamVolume(1, midVol);
            if (sync.StreamCount > 2)
                sync.SetSyncStreamVolume(2, farVol);
        }
	}
}
