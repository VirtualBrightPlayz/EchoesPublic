using System;
using Godot;

[GlobalClass]
public partial class Scp207 : WorldItem
{
    [Export]
    public bool empty = false;
    [Export]
    public MeshInstance3D inst;
    [Export]
    public GameSound sound;
    [Export]
    public double drinkDelay;

    public override void _Process(double delta)
    {
        base._Process(delta);
        // MeshInstance3D inst = GetNodeOrNull<MeshInstance3D>("%Liquid");
        // inst.Visible = !empty;
        if (IsInstanceValid(inst))
        {
            inst.Visible = !empty;
            /*
            ShaderMaterial mat = (ShaderMaterial)inst.GetActiveMaterial(0);
            if (IsInstanceValid(mat))
            {
                float val = mat.GetShaderParameter("fill_amount").AsSingle();
                val = Mathf.Lerp(val, empty ? 0f : 1f, (float)delta);
                mat.SetShaderParameter("fill_amount", val);
            }
            */
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Item.PrimaryHolder != null && Item.Player.HasAuthority && Item.PrimaryHolder.InputPrimary.HasFlag(ButtonInputFlags.JustPressed) && !empty)
        {
            EventItemUsing evt = EventManager.GetInstance<EventItemUsing>();
            if (!Emit(evt))
            {
                return;
            }
            Rpc(MethodName.RpcDrink);
            EventItemUsed evt2 = EventManager.GetInstance<EventItemUsed>();
            Emit(evt2);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public async void RpcDrink()
    {
        if (IsMultiplayerAuthority() && Item.SenderIsPlayer)
        {
            if (empty)
                return;
            EventItemUsing evt = EventManager.GetInstance<EventItemUsing>();
            if (!Emit(evt))
            {
                return;
            }
            empty = true;
            Rpc(MethodName.RpcOnDrink);
            await ToSignal(GetTree().CreateTimer(drinkDelay), SceneTreeTimer.SignalName.Timeout);
            BasePlayer player = Item.Player;
            player.statusEffectManager.EnableEffect(EffectType.Scp207, 30f, 1f);
            int soundIndex = player.Data.Sounds.IndexOf(sound);
            if (soundIndex != -1)
            {
                player.Rpc(nameof(BasePlayer.CL_PlaySoundAt3D), soundIndex, player.PlayerPosition);
            }
            EventItemUsed evt2 = EventManager.GetInstance<EventItemUsed>();
            Emit(evt2);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcOnDrink()
    {
        if (Item.viewModel is ViewmodelCup view)
        {
            view.ViewmodelEvent(Cup.ViewmodelEventDrink);
        }
    }
}
