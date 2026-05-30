using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public partial class PlayerStatusEffectManager : Node
{
    [Export]
    public BasePlayer player;

    private List<StatusEffectBase> _effects = new List<StatusEffectBase>();

    public ImmutableList<StatusEffectBase> GetEffects(Predicate<StatusEffectBase> predicate)
    {
        return _effects.Where(x => predicate(x)).ToImmutableList();
    }

    public bool IsActive(EffectType effectType)
    {
        return _effects.Any(x => x.Type == effectType && x.Active);
    }
    
    public ImmutableList<StatusEffectBase> GetEffects()
    {
        return GetEffects(x => true);
    }

    public void DisableEffects(Predicate<StatusEffectBase> predicate)
    {
        foreach (var effect in _effects.Where(x => predicate(x)))
        {
            effect.Disable();
        }
    }
    
    public void LoadEffects()
    {
        var effects = new Dictionary<EffectType, StatusEffectBase>();
        foreach (var effect in ItemManager.Instance.Data.StatusEffects)
        {
            if (effects.ContainsKey(effect.Type))
                throw new Exception($"Duplicate effect type {effect.Type} found!");
            var newEffect = (StatusEffectBase)effect.Duplicate();
            newEffect.OwnerPlayer = player;
            _effects.Add(newEffect);
        }
        //_effects = effects.Values.ToList();
    }

    public override void _Ready()
    {
        base._Ready();
        LoadEffects();
        Multiplayer.PeerConnected += MultiplayerOnPeerConnected;
    }

    private void MultiplayerOnPeerConnected(long id)
    {
        foreach (var effect in _effects)
        {
            RpcId(id, nameof(PlayerStatusEffectManager.ClientApplyStatusEffect), (int)effect.Type, effect.Duration, effect.Intensity);
        }
    }
    
    public override void _ExitTree()
    {
        base._ExitTree();
        foreach (var effect in _effects)
        {
            effect.Disable();
            effect.Dispose();
        }
        _effects.Clear();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        foreach(var effect in _effects)
        {
            if(effect.Active)
            {
                effect._Process(delta);
            }
        }
    }

    public void DisableEffects()
    {
        if (!Multiplayer.IsServer())
        {
            throw new NotServerException();
        }
        foreach (var effectType in _effects.Select(x => x.Type).Distinct())
        {
            DisableEffect(effectType);
        }
    }

    public StatusEffectBase StackEffect(EffectType effectType, float newDuration, float addedIntensity)
    {
        if (!Multiplayer.IsServer())
        {
            throw new NotServerException();
        }
        StatusEffectBase effect = _effects.Find(x => x.Type == effectType);
        if(effect == null)
        {
            Log.PrintErr($"Effect type {effectType} isn't found.");
            return null;
        }
        float intensity = addedIntensity;
        if (effect.Active)
            intensity += effect.Intensity;
        effect.Enable(newDuration, intensity);
        Rpc(nameof(PlayerStatusEffectManager.ClientApplyStatusEffect), (int)effectType, newDuration, intensity);
        return effect;
    }
    
    public StatusEffectBase EnableEffect(EffectType effectType, float duration, float intensity)
    {
        if (!Multiplayer.IsServer())
        {
            throw new NotServerException();
        }
        StatusEffectBase effect = _effects.Find(x => x.Type == effectType);
        if(effect == null)
        {
            Log.PrintErr($"Effect type {effectType} isn't found.");
            return null;
        }
        if (effect.Active)
            return effect;
        effect.Enable(duration, intensity);
        Rpc(nameof(PlayerStatusEffectManager.ClientApplyStatusEffect), (int)effectType, duration, intensity);
        return effect;
        //player.Hud.EffectOverlayManager.Rpc(nameof(EffectOverlayManager.ClientApplyStatusEffect), (int)effectType);
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void ClientApplyStatusEffect(int type, float duration, float intensity)
    {
        StatusEffectBase effect = _effects.Find(x => x.Type == (EffectType)type);
        if (effect == null)
        {
            Log.PrintErr($"Effect type {(EffectType)type} isn't found.");
            return;
        }
        effect.Enable(duration, intensity);
    }

    public StatusEffectBase DisableEffect(EffectType effectType)
    {
        if (!Multiplayer.IsServer())
        {
            throw new NotServerException();
        }
        StatusEffectBase effect = _effects.Find(x => x.Type == effectType);
        if (effect == null)
        {
            Log.PrintErr($"Effect type {effectType} isn't found.");
            return null;
        }
        if (!effect.Active)
            return effect;
        effect.Disable();
        Rpc(nameof(PlayerStatusEffectManager.ClientDisableStatusEffect), (int)effectType);
        return effect;
        //player.Hud.EffectOverlayManager.Rpc(nameof(EffectOverlayManager.ClientRemoveStatusEffect), (int)effectType);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void ClientDisableStatusEffect(int type)
    {
        StatusEffectBase effect = _effects.Find(x => x.Type == (EffectType)type);
        if (effect == null)
        {
            Log.PrintErr($"Effect type {(EffectType)type} isn't found.");
            return;
        }
        effect.Disable();
    }
}
