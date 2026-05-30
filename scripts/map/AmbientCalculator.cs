using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class AmbientCalculator : Node
{
    public struct AdditiveValue<T> where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.ISubtractionOperators<T, T, T>, System.Numerics.IMultiplyOperators<T, float, T>, System.Numerics.IDivisionOperators<T, float, T>
    {
        public T TotalValue;
        public float Count;

        public AdditiveValue(T value)
        {
            TotalValue = value;
            Count = 0;
        }

        public AdditiveValue(T value, float count)
        {
            TotalValue = value;
            Count = count;
        }

        public T CalcValue()
        {
            if (Mathf.IsZeroApprox(Count))
            {
                return TotalValue;
            }
            return TotalValue / Count;
        }

        public AdditiveValue<T> Add(T b, float amount)
        {
            if (Mathf.IsZeroApprox(Count))
                return new AdditiveValue<T>(b * amount, amount);
            return new AdditiveValue<T>(TotalValue + b * amount, Count + amount);
        }

        public static AdditiveValue<T> operator +(AdditiveValue<T> a, T b)
        {
            if (Mathf.IsZeroApprox(a.Count))
                return new AdditiveValue<T>(b, 1);
            return new AdditiveValue<T>(a.TotalValue + b, a.Count + 1);
        }

        public static AdditiveValue<T> operator -(AdditiveValue<T> a, T b)
        {
            if (Mathf.IsZeroApprox(a.Count))
                return new AdditiveValue<T>(b * -1f, 1);
            return new AdditiveValue<T>(a.TotalValue - b, a.Count + 1);
        }
    }

    public struct AdditiveColor
    {
        public Color TotalValue;
        public float Count;

        public AdditiveColor(Color value)
        {
            TotalValue = value;
            Count = 0;
        }

        public AdditiveColor(Color value, float count)
        {
            TotalValue = value;
            Count = count;
        }

        public Color CalcValue()
        {
            if (Mathf.IsZeroApprox(Count))
            {
                return TotalValue;
            }
            return TotalValue / Count;
        }

        public AdditiveColor Add(Color b, float amount)
        {
            if (Mathf.IsZeroApprox(Count))
                return new AdditiveColor(b * amount, amount);
            return new AdditiveColor(TotalValue + b * amount, Count + amount);
        }

        public static AdditiveColor operator +(AdditiveColor a, Color b)
        {
            if (Mathf.IsZeroApprox(a.Count))
                return new AdditiveColor(b, 1);
            return new AdditiveColor(a.TotalValue + b, a.Count + 1);
        }

        public static AdditiveColor operator -(AdditiveColor a, Color b)
        {
            if (Mathf.IsZeroApprox(a.Count))
                return new AdditiveColor(-b, 1);
            return new AdditiveColor(a.TotalValue - b, a.Count + 1);
        }
    }

    public static AmbientCalculator Instance { get; private set; }

    [Export]
    public NodePath Target;
    [Export]
    public NodePath VignetteOverlayTarget;
    // [Export]
    // public NodePath FilmGrainOverlayTarget;
    [Export]
    public Godot.Collections.Array<AmbientProfile> Profiles = new Godot.Collections.Array<AmbientProfile>();
    // [Export]
    public List<(AmbientProfile, float)> ComputedProfiles = new List<(AmbientProfile, float)>();

    [Signal]
    public delegate void ComputeProfilesEventHandler();

    public Environment Environment
    {
        get
        {
            var targetNode = GetNodeOrNull(Target);
            if (IsInstanceValid(targetNode))
            {
                if (targetNode is Viewport vp)
                {
                    return vp.FindWorld3D().Environment;
                }
                else if (targetNode is Camera3D cam)
                {
                    return cam.Environment;
                }
            }
            if (IsInstanceValid(MenuManager.Instance))
                return MenuManager.Instance.environment.Environment;
            return GetViewport().FindWorld3D().Environment;
        }
    }

    public bool VignetteEnabled
    {
        get
        {
            var targetNode = GetNodeOrNull(VignetteOverlayTarget);
            if (targetNode is GeometryInstance3D geom)
            {
                return geom.Visible;
            }
            else if (targetNode is CanvasItem item)
            {
                return item.Visible;
            }
            return false;
        }
        set
        {
            var targetNode = GetNodeOrNull(VignetteOverlayTarget);
            if (targetNode is GeometryInstance3D geom)
            {
                geom.Visible = value;
            }
            else if (targetNode is CanvasItem item)
            {
                item.Visible = value;
            }
        }
    }

    public float VignetteAmount
    {
        get => GetOverlayArg(VignetteOverlayTarget, "amount").AsSingle();
        set => SetOverlayArg(VignetteOverlayTarget, "amount", value);
    }

    public Color VignetteColor
    {
        get => GetOverlayArg(VignetteOverlayTarget, "main_color").AsColor();
        set => SetOverlayArg(VignetteOverlayTarget, "main_color", value);
    }

    public AmbientProfile workingProfile;

    private List<AmbientEnv.EnvData> selectedZones = new List<AmbientEnv.EnvData>();
    public List<AmbientEnvZone> zones2 => GetTree().GetNodesInGroup(AmbientEnvZone.GroupName).Where(x => x is AmbientEnvZone).Select(x => x as AmbientEnvZone).ToList();

    public override void _Ready()
    {
        Instance = this;
        workingProfile = new AmbientProfile();
    }

    public override void _Process(double delta)
    {
        CalcProfiles();
        CalcAmbient();
    }

    public Variant GetOverlayArg(NodePath path, StringName argName)
    {
        var targetNode = GetNodeOrNull(path);
        if (targetNode is GeometryInstance3D geom)
        {
            return geom.GetInstanceShaderParameter(argName);
        }
        else if (targetNode is CanvasItem item)
        {
            return item.GetInstanceShaderParameter(argName);
        }
        return default;
    }

    public void SetOverlayArg(NodePath path, StringName argName, Variant value)
    {
        var targetNode = GetNodeOrNull(path);
        if (targetNode is GeometryInstance3D geom)
        {
            geom.SetInstanceShaderParameter(argName, value);
        }
        else if (targetNode is CanvasItem item)
        {
            item.SetInstanceShaderParameter(argName, value);
        }
    }

    public void CalcProfiles()
    {
        selectedZones.Clear();
        ComputedProfiles.Clear();
        foreach (var zone in zones2)
        {
            float amount = zone.GetAmount();
            // if (amount <= 0f)
            //     continue;
            ComputedProfiles.Add((zone.profile, amount));
            // selectedZones.Add(new AmbientEnv.EnvData(zone.env, amount, zone.priority));
        }
        EmitSignalComputeProfiles();
        // AmbientEnv.ComputeFog(Environment, selectedZones.OrderBy(x => x.priority).ToArray());
    }

    public void CalcAmbient()
    {
        if (!IsInstanceValid(MenuManager.Instance))
            return;
        Environment baseEnv = MenuManager.Instance.GetEnv();
        if (!IsInstanceValid(baseEnv))
            return;
        // adjustments
        AdditiveValue<float> brightness = new AdditiveValue<float>(1f);
        AdditiveValue<float> saturation = new AdditiveValue<float>(1f);
        AdditiveValue<float> contrast = new AdditiveValue<float>(1f);
        // depth fog
        bool hasFog = false;
        // AdditiveValue<float> depthNear = new AdditiveValue<float>(10f);
        // AdditiveValue<float> depthFar = new AdditiveValue<float>(100f);
        float depthNear = baseEnv.FogDepthBegin;
        float depthFar = baseEnv.FogDepthEnd;
        // AdditiveColor depthColor = new AdditiveColor(Colors.Black);
        Color depthColor = baseEnv.FogLightColor;
        // vignette
        bool hasVignette = false;
        AdditiveValue<float> vAmount = new AdditiveValue<float>(0.5f);
        AdditiveColor vColor = new AdditiveColor(Colors.Black);

        foreach (var kvp in Profiles)
        {
            AmbientProfile profile = kvp;
            if (!IsInstanceValid(profile))
                continue;
            if (profile.HasAdjustments)
            {
                brightness += profile.Brightness;
                saturation += profile.Saturation;
                contrast += profile.Contrast;
            }
            if (profile.HasVignette)
            {
                hasVignette = true;
                vAmount += profile.VignetteAmount;
            }
            if (profile.HasVignetteColor)
            {
                vColor += profile.VignetteColor;
            }
        }

        float brightnessFinal = brightness.CalcValue();
        float saturationFinal = saturation.CalcValue();
        float contrastFinal = contrast.CalcValue();

        foreach (var kvp in ComputedProfiles)
        {
            AmbientProfile profile = kvp.Item1;
            if (!IsInstanceValid(profile))
                continue;
            if (profile.HasAdjustments)
            {
                brightnessFinal = Mathf.Lerp(brightnessFinal, profile.Brightness, kvp.Item2);
                saturationFinal = Mathf.Lerp(saturationFinal, profile.Saturation, kvp.Item2);
                contrastFinal = Mathf.Lerp(contrastFinal, profile.Contrast, kvp.Item2);
            }
            if (profile.HasDepthFog)
            {
                hasFog = true;
                depthNear = Mathf.Lerp(depthNear, profile.DepthFogNear, kvp.Item2);
                depthFar = Mathf.Lerp(depthFar, profile.DepthFogFar, kvp.Item2);
            }
            if (profile.HasDepthFogColor)
            {
                depthColor = depthColor.Lerp(profile.DepthFogColor, kvp.Item2);
            }
            if (profile.HasVignette)
            {
                hasVignette = true;
                vAmount += profile.VignetteAmount;
            }
            if (profile.HasVignetteColor)
            {
                vColor += profile.VignetteColor;
            }
        }
        // adjustments
        Environment.AdjustmentEnabled = true;
        Environment.AdjustmentBrightness = brightnessFinal;
        Environment.AdjustmentSaturation = saturationFinal;
        Environment.AdjustmentContrast = contrastFinal;
        // depth fog
        if (Environment.FogMode == Environment.FogModeEnum.Depth)
        {
            // Environment.FogEnabled = hasFog;
            // Environment.FogMode = Environment.FogModeEnum.Depth;
            Environment.FogDepthBegin = depthNear;//.CalcValue();
            Environment.FogDepthEnd = depthFar;//.CalcValue();
            depthColor.A = 1f;
            Environment.FogLightColor = depthColor;//.CalcValue();
        }
        // vignette
        VignetteEnabled = hasVignette && !MenuManager.Instance.IsXR;
        VignetteAmount = vAmount.CalcValue();
        VignetteColor = vColor.CalcValue();
    }
}