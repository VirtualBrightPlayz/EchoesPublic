using System;
using Godot;

[GlobalClass]
public abstract partial class StatusEffectBase : Resource
{
    [Export]
    public PackedScene Overlay;

    public abstract EffectType Type { get; }

    [Obsolete]
    public virtual bool Negative { get; } = false;

    public abstract EffectClassification Classification { get; }
    
    private IDamageSource _damageSource;

    public IDamageSource Source
    {
        get
        {
            if (_damageSource == null)
            {
                _damageSource = OwnerPlayer;
            }
            return _damageSource;
        }
        set
        {
            _damageSource = value;
        }
    }
    
    private bool _active = false;

    public bool Active
    {
        get
        {
            return _active;
        }
        set
        {
            _active = value;
            if (value)
            {
                Disable();
            }
            else
            {
                Enable(1f);
            }
        }
    }

    private float _intensity = 0;

    public float Intensity
    {
        get
        {
            return _intensity;
        }
        set
        {
            this._intensity = value;
            if(_intensity == 0f)
            {
                Disable();
            }
        }
    }

    public void Enable(float duration, float intensity = 1f)
    {
        this._duration = duration;
        this._intensity = intensity;
        this._active = true;
        if (OwnerPlayer.IsServer)
        {
            ServerEnabled();
        }
        if (OwnerPlayer.IsLocalPlayer)
        {
            LocalClientEnabled();
        }
        ClientEnabled();
    }

    public void Disable()
    {
        this._active = false;
        this._duration = 0f;
        this._intensity = 0f;
        if (OwnerPlayer.IsServer)
        {
            ServerDisabled();
        }
        if (OwnerPlayer.IsLocalPlayer)
        {
            LocalClientDisabled();
        }
        ClientDisabled();
    }

    private double _duration;

    public double Duration
    {
        get
        {
            return _duration;
        }
    }

    public virtual void _Process(double delta)
    {
        _duration -= delta;
        if(_duration <= 0)
        {
            Disable();
            return;
        }
    }

    protected BasePlayer _owner;

    public BasePlayer OwnerPlayer
    {
        get { return _owner; }
        set
        {
            if(_owner != null)
            {
                return;
            }
            _owner = value;
        }
    }

    protected virtual void ClientEnabled()
    {
        
    }

    protected virtual void ClientDisabled()
    {
        
    }
    
    protected virtual void ServerEnabled()
    {
        
    }

    protected virtual void LocalClientEnabled()
    {
        if (OwnerPlayer is NetworkPlayer plr)
        {
            plr.Hud.EffectOverlayManager.ApplyOverlay(this);
        }
    }

    protected virtual void ServerDisabled()
    {
        
    }

    protected virtual void LocalClientDisabled()
    {
        if (OwnerPlayer is NetworkPlayer plr)
        {
            plr.Hud.EffectOverlayManager.RemoveOverlay(this);
        }
    }

    public StatusEffectBase() { }
}