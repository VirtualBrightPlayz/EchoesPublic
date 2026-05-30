using Godot;

[GlobalClass]
public partial class Scp1930 : WorldItem, IHealSource, IDamageSource, ISpecificEventSource<IScp1930Event>
{
    [Export]
    public float healAmount = 50f;
    [Export]
    public float damageAmount = 50f;

    public override string AttackerDisplayName => "SCP-1930";

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcHeal()
    {
        if (IsMultiplayerAuthority() && Item.SenderIsPlayer)
        {
            double rng = GD.RandRange(0d, 1d);
            Scp1930Decision decision = Scp1930Decision.Heal;
            Event1930Deciding evt = EventManager.GetInstance<Event1930Deciding>();
            
            if (rng <= 0.075d)
            {
                decision = Scp1930Decision.Kill;
                evt.Amount = float.PositiveInfinity;
            }
            else if (rng <= 0.175d)
            {
                decision = Scp1930Decision.Damage;
                evt.Amount = damageAmount;
            }
            else if (rng <= 0.75d)
            {
                decision = Scp1930Decision.Heal;
                evt.Amount = healAmount;
            }
            evt.Decision = decision;

            if(!Emit(evt))
            {
                return;
            }

            switch (evt.Decision)
            {
                case Scp1930Decision.Kill:
                    Item.Player.Kill(new DamageInfo(0f, this, DamageType.Generic));
                    break;
                case Scp1930Decision.Damage:
                    Item.Player.Damage(new DamageInfo(evt.Amount, this, DamageType.Generic));
                    break;
                case Scp1930Decision.Heal:
                    Item.Player.Heal(new HealInfo(evt.Amount, this));
                    break;
            }
            // Item.Player.statusEffectManager.StackEffect(EffectType.Scp1930, 1440f, 0.1f);
        }
    }

    public enum Scp1930Decision
    {
        Kill,
        Damage,
        Heal
    }

    public void SendHeal()
    {
        Rpc(MethodName.RpcHeal);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (Item.ViewModelEnabledCached)
        {
            return;
        }
        if (Item.PrimaryHolder != null && Item.Player.HasAuthority && Item.PrimaryHolder.InputPrimary.HasFlag(ButtonInputFlags.JustPressed))
        {
            Rpc(MethodName.RpcHeal);
        }
    }

    public bool Emit(IScp1930Event evt)
    {
        evt.WorldItem = this;
        evt.Scp1930 = this;
        return Emit(evt as IWorldItemEvent);
    }
}
