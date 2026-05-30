using Godot;

public partial class VoiceOverlayUI : PanelContainer
{
    [Export]
    public StyleBoxFlat styleBox;
    [Export]
    public RichTextLabel label;
    private StyleBoxFlat localBox;
    public NetworkPlayer player;

    public override void _Ready()
    {
        Visible = false;
        localBox = (StyleBoxFlat)styleBox.Duplicate();
        AddThemeStyleboxOverride("panel", localBox);
    }

    public override void _Process(double delta)
    {
        if (IInitScript.IsServerOnly || !IsInstanceValid(player) || !IsInstanceValid(player.voiceChat))
        {
            return;
        }
        label.Text = $"[center]{player?.username?.Replace("[", "[lb]") ?? "N/A"}[/center]";
        Visible = player.voiceChat.IsSpeaking && !player.HasAuthority && player.voiceChat.GetVoice().GetMeta(Intercom.IntercomName, false).AsBool() && !player.voiceChat.GetVoice().GetMeta(Intercom.MuteName, false).AsBool();
        var col = localBox.BgColor;
        col.A = player.voiceChat.Loudness * 80f;
        if (Visible)
        {
            localBox.BgColor = col;
        }
    }
}
