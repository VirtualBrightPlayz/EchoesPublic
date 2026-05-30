using Godot;

[GlobalClass]
[Tool]
public partial class AmbientProfile : Resource
{
    [ExportGroup("Adjustments")]
    [Export]
    public bool HasAdjustments = false;
    [Export]
    public float Brightness = 1f;
    [Export]
    public float Saturation = 1f;
    [Export]
    public float Contrast = 1f;

    [ExportGroup("Depth Fog")]
    [Export]
    public bool HasDepthFog = false;
    [Export]
    public float DepthFogNear = 10f;
    [Export]
    public float DepthFogFar = 100f;
    [Export]
    public bool HasDepthFogColor = false;
    [Export]
    public Color DepthFogColor = Colors.Black;

    [ExportGroup("Vignette")]
    [Export]
    public bool HasVignette = false;
    [Export]
    public float VignetteAmount = 0.5f;
    [Export]
    public bool HasVignetteColor = false;
    [Export]
    public Color VignetteColor = Colors.Black;

    public void Lerp(AmbientProfile primary, AmbientProfile other, float t)
    {
        AmbientProfile newProfile = this;

        newProfile.HasAdjustments = primary.HasAdjustments | other.HasAdjustments;
        newProfile.Brightness = Mathf.Lerp(primary.Brightness, other.Brightness, t);
        newProfile.Saturation = Mathf.Lerp(primary.Saturation, other.Saturation, t);
        newProfile.Contrast = Mathf.Lerp(primary.Contrast, other.Contrast, t);

        newProfile.HasDepthFog = primary.HasDepthFog | other.HasDepthFog;
        newProfile.DepthFogNear = Mathf.Lerp(primary.DepthFogNear, other.DepthFogNear, t);
        newProfile.DepthFogFar = Mathf.Lerp(primary.DepthFogFar, other.DepthFogFar, t);
        newProfile.HasDepthFogColor = primary.HasDepthFogColor | other.HasDepthFogColor;
        newProfile.DepthFogColor = primary.DepthFogColor.Lerp(other.DepthFogColor, t);

        newProfile.HasVignette = primary.HasVignette | other.HasVignette;
        newProfile.VignetteAmount = Mathf.Lerp(primary.VignetteAmount, other.VignetteAmount, t);
        newProfile.HasVignetteColor = primary.HasVignetteColor | other.HasVignetteColor;
        newProfile.VignetteColor = primary.VignetteColor.Lerp(other.VignetteColor, t);
    }
}