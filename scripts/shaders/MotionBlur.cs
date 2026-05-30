using Godot;
using System;

[Tool]
public partial class MotionBlur : MeshInstance3D
{
    [Export]
    public SubViewport viewport;
    [Export]
    public Camera3D finalCam;
    public Vector3 cam_pos_prev;
    public Quaternion cam_rot_prev;
    private ImageTexture tex;

    [Export]
    public float Multiplier = 1f;

    public override void _Ready()
    {
        // tex = new ImageTexture();
        RenderingServer.FramePostDraw += Draw;
    }

    public override void _ExitTree()
    {
        tex?.Dispose();
        RenderingServer.FramePostDraw -= Draw;
    }

    private void Draw()
    {
        if (GetSurfaceOverrideMaterialCount() <= 0 || !Visible)
            return;
        ShaderMaterial mat = GetSurfaceOverrideMaterial(0) as ShaderMaterial;
        Camera3D cam = GetParent() as Camera3D;
        if (cam == null || mat == null)
            return;
        if (viewport == null)
            return;

        // mat.SetShaderParameter("screen_texture", viewport.GetTexture());
    }

    public override void _Process(double delta)
    {
        if (GetSurfaceOverrideMaterialCount() <= 0 || !Visible)
            return;
        ShaderMaterial mat = GetSurfaceOverrideMaterial(0) as ShaderMaterial;
        Camera3D cam = GetParent() as Camera3D;
        if (cam == null || mat == null)
            return;
        if (viewport == null || finalCam == null)
            return;

        Vector3 velocity = cam.GlobalPosition - cam_pos_prev;
        Quaternion cam_rot = new Quaternion(cam.GlobalTransform.Basis);
        Quaternion cam_rot_diff = cam_rot - cam_rot_prev;
        if (cam_rot.Dot(cam_rot_prev) < 0f)
        {
            cam_rot_diff = -cam_rot_diff;
        }

        Quaternion cam_rot_conj = Conjugate(cam_rot);
        Vector3 ang_vel = Trunc3(cam_rot_diff * 2f) * cam_rot_conj;

        mat.SetShaderParameter("linear_velocity", velocity * Multiplier);
        mat.SetShaderParameter("angular_velocity", ang_vel * Multiplier);

        RenderingServer.ViewportAttachCamera(viewport.GetViewportRid(), finalCam.GetCameraRid());

        // Rid rid = RenderingServer.ViewportGetTexture(cam.GetViewport().GetViewportRid());
        // Rid rid = RenderingServer.ViewportGetTexture(viewport.GetViewportRid());
        // RenderingServer.MaterialSetParam(mat.GetRid(), "screen_texture", rid);
        // mat.SetShaderParameter("screen_texture", cam.GetViewport().GetTexture());
        mat.SetShaderParameter("screen_texture", viewport.GetTexture());

        finalCam.GlobalPosition = cam.GlobalPosition;
        finalCam.GlobalRotation = cam.GlobalRotation;

        cam_pos_prev = cam.GlobalPosition;
        cam_rot_prev = new Quaternion(cam.GlobalTransform.Basis);
    }

    public static Quaternion Conjugate(Quaternion quat)
    {
        return new Quaternion(-quat.X, -quat.Y, -quat.Z, quat.W);
    }

    public static Vector3 Trunc3(Quaternion quat)
    {
        return new Vector3(quat.X, quat.Y, quat.Z);
    }
}
