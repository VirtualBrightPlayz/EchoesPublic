using System;
using Godot;

[Tool]
[GlobalClass]
public partial class LightSettings : Node3D
{
    [Export]
    public bool UseChildLights { get; set; } = true;
    [Export(PropertyHint.Range, "0,4096,or_greater")]
    public float Range { get; set; } = 10f;
    [Export(PropertyHint.Range, "0,4096,or_greater")]
    public float SecondaryRange { get; set; } = 5f;
    [Export(PropertyHint.Range, "0,4096,or_greater")]
    public float FadeRangePadding { get; set; } = 0f;
    [Export]
    public bool Shadows { get; set; } = true;
    [Export]
    public Light3D.BakeMode BakeMode { get; set; } = Light3D.BakeMode.Static;
    [Export(PropertyHint.ColorNoAlpha)]
    public Color Color { get; set; } = Colors.White;
    [Export(PropertyHint.Range, "0,1,or_greater")]
    public float LightSize { get; set; } = 0f;
    [Export(PropertyHint.Range, "0,1,or_greater")]
    public float SecondaryLightSize { get; set; } = 0f;
    [Export(PropertyHint.Range, "-10,10,or_lesser,or_greater")]
    public float PrimaryAttenuation { get; set; } = 1f;
    [Export(PropertyHint.Range, "-10,10,or_lesser,or_greater")]
    public float SecondaryAttenuation { get; set; } = 1f;
    [Export(PropertyHint.Range, "-10,10,or_lesser,or_greater")]
    public float PrimaryAttenuationAngle { get; set; } = 1f;
    [Export(PropertyHint.Range, "-10,10,or_lesser,or_greater")]
    public float SecondaryAttenuationAngle { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,16,or_lesser,or_greater")]
    public float PrimaryEnergy { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,16,or_lesser,or_greater")]
    public float SecondaryEnergy { get; set; } = 1f;
    
    [Export(PropertyHint.Range, "0,16,or_lesser,or_greater")]
    public float PrimaryVolumetricFogEnergy { get; set; } = 1f;
    
    [Export(PropertyHint.Range, "0,16,or_lesser,or_greater")]
    public float SecondaryVolumetricFogEnergy { get; set; } = 1f;
    [Export(PropertyHint.Range, "-360,360,or_lesser,or_greater")]
    public float PrimarySpotLightAngle { get; set; } = 45f;
    [Export(PropertyHint.Range, "-360,360,or_lesser,or_greater")]
    public float SecondarySpotLightAngle { get; set; } = 45f;
    [ExportGroup("Config")]
    [Export(PropertyHint.Range, "0,4096,or_greater")]
    public float FadeLength { get; set; } = 10f;
    [Export(PropertyHint.Range, "0,4096,or_greater")]
    public float FadeRangeShadows { get; set; } = 3f;

    
    [Export]
    public Godot.Collections.Array<Light3D> PrimaryLights = new Godot.Collections.Array<Light3D>();
    [Export]
    public Godot.Collections.Array<Light3D> SecondaryLights = new Godot.Collections.Array<Light3D>();

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            UpdateLights();
        }
        else
        {
            UpdateLights();
            if (RenderingServer.GetCurrentRenderingMethod().Equals("gl_compatibility"))
            {
                bool shadow = Settings.User.ShadowQuality != Settings.GenericQuality.VeryLow;
                {
                    for (int i = 0; i < PrimaryLights.Count; i++)
                    {
                        PrimaryLights[i].ShadowEnabled = Shadows && shadow;
                    }
                }
            }
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            UpdateLights();
        }
    }

    public void UpdateLights()
    {
        for (int i = 0; i < PrimaryLights.Count; i++)
        {
            UpdateLight(PrimaryLights[i], true);
        }
        for (int i = 0; i < SecondaryLights.Count; i++)
        {
            UpdateLight(SecondaryLights[i], false);
        }
    }

    public void UpdateLight(Light3D light, bool primary)
    {
        float range = primary ? Range : SecondaryRange;
        float size = primary ? LightSize : SecondaryLightSize;
        if (IsInstanceValid(light))
        {
            light.Visible = UseChildLights;
            light.DistanceFadeEnabled = true;
            light.DistanceFadeLength = FadeLength;
            light.DistanceFadeBegin = range + FadeRangePadding;
            light.DistanceFadeShadow = range + FadeRangeShadows;
            light.ShadowEnabled = Shadows && primary;
            light.LightBakeMode = BakeMode;
            light.LightColor = Color;
            light.LightSize = size;
            light.LightVolumetricFogEnergy = primary ? PrimaryVolumetricFogEnergy : SecondaryVolumetricFogEnergy;
            light.LightEnergy = primary ? PrimaryEnergy : SecondaryEnergy;
            switch (light)
            {
                case OmniLight3D omni:
                    omni.OmniRange = range;
                    omni.OmniAttenuation = primary ? PrimaryAttenuation : SecondaryAttenuation;
                    break;
                case SpotLight3D spot:
                    spot.SpotRange = range;
                    spot.SpotAttenuation = primary ? PrimaryAttenuation : SecondaryAttenuation;
                    spot.SpotAngleAttenuation = primary ? PrimaryAttenuationAngle : SecondaryAttenuationAngle;
                    spot.SpotAngle = primary ? PrimarySpotLightAngle : SecondarySpotLightAngle;
                    break;
            }
        }
    }
}