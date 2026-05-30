using Godot;

public abstract partial class TickingStatusEffect : StatusEffectBase
{
    protected override void LocalClientEnabled()
    {
        base.LocalClientEnabled();
        ClientTick();
    }

    protected override void ServerEnabled()
    {
        base.ServerEnabled();
        _totalDeltaTime = 0d;
        _totalTicks = 0;
    }

    protected override void ServerDisabled()
    {
        base.ServerDisabled();
        _totalDeltaTime = 0d;
        _totalTicks = 0;
    }

    private double _totalDeltaTime = 0d;

    private bool firstActiveFrame = false;
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        if(!Active)
        {
            return;
        }
        if (!firstActiveFrame)
        {   
            firstActiveFrame = true;
            ServerTick();
        }
        if (_totalDeltaTime >= 1.0d)
        {
            _totalDeltaTime = 0d;
            _totalTicks++;
            if (OwnerPlayer.IsServer)
            {
                ServerTick();
            }
            if (OwnerPlayer.IsLocalPlayer)
            {
                ClientTick();
            }
        }
        else
        {
            _totalDeltaTime += delta;
        }
    }

    protected int _totalTicks;

    protected virtual void ServerTick()
    {
        //Log.Print("ServerTick");
    }

    protected virtual void ClientTick()
    {
        //Log.Print("ClientTick");
    }
}