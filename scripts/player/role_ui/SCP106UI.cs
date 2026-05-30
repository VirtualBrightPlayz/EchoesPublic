using Godot;

public partial class SCP106UI : Control
{
    [Export]
    public ColorRect overlay;
    [Export]
    public Control inventoryRoot;
    [Export]
    public PortalAbility ability;

    public async void Teleported(float time)
    {
        overlay.Visible = true;
        var tween = CreateTween();
        var clear = new Color(Colors.Black, 0f);
        tween.TweenProperty(overlay, new NodePath(ColorRect.PropertyName.Color), Colors.Black, time / 3f).From(clear);
        tween.TweenProperty(overlay, new NodePath(ColorRect.PropertyName.Color), clear, time / 3f).From(Colors.Black).SetDelay(time / 3f * 2f);
        await ToSignal(tween, Tween.SignalName.Finished);
        overlay.Visible = false;
    }

    public override void _Ready()
    {
        overlay.Visible = false;
    }

    public void PlacePortal()
    {
        ability.RpcId(MultiplayerPeer.TargetPeerServer, PortalAbility.MethodName.PlacePortal);
        CloseUI();
    }

    public void UsePortal()
    {
        ability.RpcId(MultiplayerPeer.TargetPeerServer, PortalAbility.MethodName.UsePortal);
        CloseUI();
    }

    public void UseRandomTP()
    {
    }

    public void CloseUI()
    {
        if (ability.Player.IsLocalPlayer)
        {
            ability.IsUIOpen = false;
            ability.Player.ChangeActionSet("InGame");
        }
    }

    public override void _Process(double delta)
    {
        if (ability.Player.IsLocalPlayer)
        {
            inventoryRoot.Visible = ability.IsUIOpen;
        }
    }
}
