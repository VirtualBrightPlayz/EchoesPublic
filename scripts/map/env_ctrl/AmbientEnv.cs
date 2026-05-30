using Godot;

[GlobalClass]
[Tool]
public partial class AmbientEnv : Resource
{
    public struct EnvData
    {
        public AmbientEnv env;
        public float amount;
        public int priority;

        public EnvData(AmbientEnv env)
        {
            this.env = env;
            this.amount = 1f;
        }

        public EnvData(AmbientEnv env, float amount, int priority)
        {
            this.env = env;
            this.amount = amount;
            this.priority = priority;
        }
    }

    [ExportGroup("Sky")]
    [Export]
    public Material skyMaterial;
    [Export]
    public float skyEnergy = 1f;
    [Export]
    public Color backgroundColor = Colors.Black;
    [Export]
    public Color ambientColor = Colors.Black;

    [ExportGroup("Fog")]
    [Export]
    public Environment.FogModeEnum fogMode = Environment.FogModeEnum.Depth;
    [Export]
    public float fogDensity = 1f;
    [Export]
    public Color fogColor = Colors.Black;
    [Export]
    public float fogDepthBegin = 10f;
    [Export]
    public float fogDepthEnd = 40f;

    public static void ApplyFog(Environment env, EnvData baseEnv, bool isBase)
    {
        if (isBase)
        {
            env.FogMode = baseEnv.env.fogMode;
            env.FogDensity = baseEnv.env.fogDensity;
            env.FogLightColor = baseEnv.env.fogColor;
            env.FogDepthBegin = baseEnv.env.fogDepthBegin;
            env.FogDepthEnd = baseEnv.env.fogDepthEnd;
        }
        else
        {
            float weight = baseEnv.amount;
            if (env.FogMode == baseEnv.env.fogMode)
            {
                env.FogDensity = Mathf.Lerp(env.FogDensity, baseEnv.env.fogDensity, weight);
                env.FogLightColor = env.FogLightColor.Lerp(baseEnv.env.fogColor, weight);
                env.FogDepthBegin = Mathf.Lerp(env.FogDepthBegin, baseEnv.env.fogDepthBegin, weight);
                env.FogDepthEnd = Mathf.Lerp(env.FogDepthEnd, baseEnv.env.fogDepthEnd, weight);
            }
        }
    }

    public static void ApplyEnv(Environment env, EnvData baseEnv, bool isBase)
    {
        if (isBase)
        {
            if (IsInstanceValid(baseEnv.env.skyMaterial))
            {
                env.BackgroundMode = Environment.BGMode.Sky;
                env.AmbientLightSource = Environment.AmbientSource.Bg;
                if (!IsInstanceValid(env.Sky))
                {
                    env.Sky = new Sky();
                }
                env.Sky.SkyMaterial = baseEnv.env.skyMaterial;
            }
            else
            {
                env.BackgroundMode = Environment.BGMode.Color;
                env.AmbientLightSource = Environment.AmbientSource.Color;
                env.BackgroundColor = baseEnv.env.backgroundColor;
                env.AmbientLightColor = baseEnv.env.ambientColor;
            }
            env.BackgroundEnergyMultiplier = baseEnv.env.skyEnergy;
            env.FogMode = baseEnv.env.fogMode;
            env.FogDensity = baseEnv.env.fogDensity;
            env.FogLightColor = baseEnv.env.fogColor;
            env.FogDepthBegin = baseEnv.env.fogDepthBegin;
            env.FogDepthEnd = baseEnv.env.fogDepthEnd;
        }
        else
        {
            float weight = baseEnv.amount;
            if (IsInstanceValid(baseEnv.env.skyMaterial))
            {
                // RIP not supported
            }
            else if (env.BackgroundMode == Environment.BGMode.Color)
            {
                env.BackgroundColor = env.BackgroundColor.Lerp(baseEnv.env.backgroundColor, weight);
                env.AmbientLightColor = env.AmbientLightColor.Lerp(baseEnv.env.ambientColor, weight);
            }
            env.BackgroundEnergyMultiplier = Mathf.Lerp(env.BackgroundEnergyMultiplier, baseEnv.env.skyEnergy, weight);
            if (env.FogMode == baseEnv.env.fogMode)
            {
                env.FogDensity = Mathf.Lerp(env.FogDensity, baseEnv.env.fogDensity, weight);
                env.FogLightColor = env.FogLightColor.Lerp(baseEnv.env.fogColor, weight);
                env.FogDepthBegin = Mathf.Lerp(env.FogDepthBegin, baseEnv.env.fogDepthBegin, weight);
                env.FogDepthEnd = Mathf.Lerp(env.FogDepthEnd, baseEnv.env.fogDepthEnd, weight);
            }
        }
    }

    public static void ComputeEnv(Environment env, params EnvData[] ambients)
    {
        for (int i = 0; i < ambients.Length; i++)
        {
            EnvData baseEnv = ambients[i];
            // baseEnv.amount = Mathf.Clamp(baseEnv.amount, 0f, 1f);
            ApplyEnv(env, baseEnv, i == 0);
        }
    }

    public static void ComputeFog(Environment env, params EnvData[] ambients)
    {
        for (int i = 0; i < ambients.Length; i++)
        {
            EnvData baseEnv = ambients[i];
            // baseEnv.amount = Mathf.Clamp(baseEnv.amount, 0f, 1f);
            ApplyFog(env, baseEnv, i == 0);
        }
    }
}