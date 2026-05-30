
using Godot;

[GlobalClass]
public partial class Flashbang : StatusEffectBase
{
    public override EffectType Type => EffectType.Flashbang;
    public override EffectClassification Classification => EffectClassification.Negative;

    protected override void LocalClientEnabled()
    {
        base.LocalClientEnabled();
        if (OwnerPlayer is NetworkPlayer player)
        {
            player.Hud.Flash(Duration);
        }
    }

    protected override void LocalClientDisabled()
    {
        base.LocalClientDisabled();
        if (OwnerPlayer is NetworkPlayer player)
        {
            player.Hud.Unflash();
        }
    }
}