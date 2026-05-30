using System.Collections.Generic;
using Godot;

public partial class EffectOverlayManager : Control 
{
    [Export]
    public BasePlayer player;

    private readonly Dictionary<EffectType, Node> _activeEffectOverlays = new Dictionary<EffectType, Node>();

    public void ApplyOverlay(StatusEffectBase effect)
    {
        if(effect.Overlay != null && !_activeEffectOverlays.ContainsKey(effect.Type))
        {
            var overlay = effect.Overlay.Instantiate<Control>();
            _activeEffectOverlays.Add(effect.Type, overlay);
            overlay.Name = effect.Type.ToString();
            AddChild(overlay);
        }
    }

    public void RemoveOverlay(StatusEffectBase effect)
    {
        if(_activeEffectOverlays.TryGetValue(effect.Type, out var overlay))
        {
            overlay.QueueFree();
            _activeEffectOverlays.Remove(effect.Type);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void ClientApplyStatusEffect(int statusEffect)
    {
        //if (Multiplayer.GetRemoteSenderId() != 1)
        //    return;
        //if (statusEffect == 0)
        //    return;
        //var effectType = (EffectType)statusEffect;
        //if (_activeEffectOverlays.ContainsKey(effectType))
        //    return;

        //var effect = StatusEffectManager.LoadedEffects[effectType];
        //if (effect is AdvancedStatusEffect advEffect)
        //{
        //    if (advEffect.OverlayScene != null)
        //    {

                
        //    }
        //}
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void ClientRemoveAllStatusEffects()
    {
        //if (Multiplayer.GetRemoteSenderId() != 1)
        //    return;
        //foreach (var child in _activeEffectOverlays.Values)
        //{
        //    child.QueueFree();
        //}

        //_activeEffectOverlays.Clear();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void ClientRemoveStatusEffect(int statusEffect)
    {
        //if (Multiplayer.GetRemoteSenderId() != 1)
        //    return;
        //var effectType = (EffectType)statusEffect;
        //if (!_activeEffectOverlays.ContainsKey(effectType))
        //    return;
        //var child = _activeEffectOverlays[effectType];
        //if (child != null)
        //{
        //    child.QueueFree();
        //}
        //_activeEffectOverlays.Remove(effectType);
    }
}