using Godot;

[GlobalClass]
public partial class LightSpark : Node
{
    [Export]
    public GameEffect effect;
    [Export]
    public double timeMin = 10d;
    [Export]
    public double timeMax = 45d;
    [Export]
    public double sparkDelay = 1d;
    [Export]
    public double sparkTime = 1d;
    private Timer timer;

    public override void _Ready()
    {
        timer = new Timer();
        AddChild(timer);
        timer.Timeout += Spark;
        timer.Start(GD.RandRange(timeMin, timeMax));
    }

    public void SetLightEnergyRng(float energy)
    {
        Light3D light = GetParent<Light3D>();
        light.LightEnergy = (float)GD.RandRange(0f, energy);
    }

    public void Spark()
    {
        Light3D light = GetParent<Light3D>();
        if (IsInstanceValid(effect))
            effect.PlayOneShot3D(light, true);
        float energy = light.LightEnergy;
        light.LightEnergy = 0f;
        Tween tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(SetLightEnergyRng), 0f, energy, sparkTime).SetTrans(Tween.TransitionType.Quad).SetDelay(sparkDelay);
        tween.TweenProperty(light, Light3D.PropertyName.LightEnergy.ToString(), energy, 0.1d);
        timer.Start(GD.RandRange(timeMin, timeMax));
    }
}