using Godot;

[GlobalClass]
public partial class Flashlight : WorldItem
{
    [Export]
    public Light3D light;
    [Export]
    public bool isOn = true;
    [Export]
    public bool CanFire = true;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        light.Visible = Item.ModelEnabledCached && isOn;
        if (Item.PrimaryHolder != null && !Item.PrimaryHolder.IsSocket && Item.Player.HasAuthority)
        {
            if (CanFire && Item.PrimaryHolder.InputPrimary.HasFlag(ButtonInputFlags.JustPressed))
            {
                if (Item.viewModel is ViewmodelFlashlight fl)
                {
                    fl.Click();
                }
                Rpc(nameof(RpcSetOn), !isOn);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcSetOn(bool onState)
    {
        if (Item.SenderIsPlayer)
        {
            isOn = onState;
        }
    }
}
