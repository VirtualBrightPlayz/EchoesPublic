using Godot;

public partial class TalkingIndicator : Node
{
    public IPlayerController player => GetParent() as IPlayerController;
    [Export]
    public Sprite3D spr;

    public override void _Process(double delta)
    {
        if (player == null || player.Player.IsLocalPlayer)
        {
            spr.Modulate = MakeColor(1f);
            return;
        }
        if (player.Player is NetworkPlayer plr)
            spr.Modulate = MakeColor(1f - plr.voiceChat.Loudness * 80f);
        else
            spr.Modulate = MakeColor(1f);
    }

    private static Color MakeColor(float alpha)
    {
        return new Color(1f, 1f, 1f, 1f - alpha);
    }
}