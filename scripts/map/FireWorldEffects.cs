using Godot;

public partial class FireWorldEffects : Node3D
{
    [Export]
    public GpuParticles3D[] particles = new GpuParticles3D[0];
    
    [Export]
    public GpuParticles3D[] disableOnlyParticles = new GpuParticles3D[0];
    
    [Export]
    public AudioStreamPlayer3D audio;
    
    [Export]
    public OmniLight3D light;

    private float _range = 0f;
    [Export(PropertyHint.Range, "0,100,0.0001,or_greater")]
    public float MaxRange
    {
        get => _range;
        set
        {
            if (_range != value)
            {
                _range = value;
                UpdateVisuals();
            }
        }
    }

    private float _amount = 1f;
    [Export(PropertyHint.Range, "0,1")]
    public float FireAmount
    {
        get => _amount;
        set
        {
            if (_amount != value)
            {
                _amount = value;
                UpdateVisuals();
            }
        }
    }

    private bool _enabled = true;

    [Export]
    public bool Enabled
    {
        get
        {
            return _enabled;
        }
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                UpdateVisuals();
            }
        }
    }

    [Export]
    public float LightFrequency = 0f;

    private float timer;

    public void UpdateVisuals()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            if (IsInstanceValid(particles[i]))
            {
                particles[i].AmountRatio = _amount;
                particles[i].Emitting = _enabled;
            }
        }
        
        for (int i = 0; i < disableOnlyParticles.Length; i++)
        {
            if (IsInstanceValid(disableOnlyParticles[i]))
            {
                disableOnlyParticles[i].Emitting = _enabled;
            }
        }
        
        if (IsInstanceValid(light))
        {
            light.OmniRange = _range;
            light.LightEnergy = _amount;
            light.Visible = _enabled;
        }
        if (IsInstanceValid(audio))
        {
            audio.MaxDistance = _range;
            audio.StreamPaused = Mathf.IsZeroApprox(_amount);
            audio.StreamPaused = !_enabled;
            audio.Visible = _enabled;
        }
    }

    public override void _Ready()
    {
        UpdateVisuals();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (IsInstanceValid(light))
        {
            timer -= (float)delta;
            if (timer <= 0f || timer > LightFrequency)
            {
                light.OmniRange = _range;
                light.LightEnergy = (float)GD.RandRange(_amount * 0.85f, _amount * 1.15f);
                light.Visible = _enabled;
            }
        }
    }
}