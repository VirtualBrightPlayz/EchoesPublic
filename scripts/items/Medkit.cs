using Godot;

[GlobalClass]
public partial class Medkit : WorldItem, ISpecificEventSource<IMedkitEvent>
{
    [Export]
    public float healAmount = 50f;

    public bool Emit(IMedkitEvent evt)
    {
        evt.Medkit = this;
        evt.WorldItem = this;
        return Emit(evt as IWorldItemEvent);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcHeal()
    {
        if (IsMultiplayerAuthority() && Item.SenderIsPlayer && Item.Player.Health < Item.Player.MaxHealth)
        {
            EventMedkitUsing evt = EventManager.GetInstance<EventMedkitUsing>();
            evt.HealAmount = healAmount;
            evt.IsClient = false;
            Emit(evt);
            if (evt.Canceled)
            {
                return;
            }
            Item.Player.Heal(new HealInfo(evt.HealAmount));
            healAmount = 0f; // prevent frame-perfect double healing
            Item.QueueFree();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Item.PrimaryHolder != null && Item.Player.HasAuthority && Item.PrimaryHolder.InputPrimary.HasFlag(ButtonInputFlags.JustPressed))
        {
            EventMedkitUsing evt = EventManager.GetInstance<EventMedkitUsing>();
            evt.HealAmount = healAmount;
            evt.IsClient = true;
            Emit(evt);
            if (evt.Canceled)
            {
                return;
            }
            Rpc(MethodName.RpcHeal);
        }
    }
}
