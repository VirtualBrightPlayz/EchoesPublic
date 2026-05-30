using Godot;

[Tool]
[GlobalClass]
public partial class UsernameEditSetting : Text
{
    public override void _Draw()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }
        base._Draw();
        if (SteamManager.Supported)
        {
            TextEdit.Text = IInitScript.Instance.Username;
            Enabled = false;
            ShowMessage(MessageLevel.Warning, "Your username is managed by steam.");
            return;
        }
        if (IsInstanceValid(NetworkPlayer.LocalInstance))
        {
            Enabled = false;
            ShowMessage(MessageLevel.Warning, "You cannot change your name while in game.");
            return;
        }
        HideMessage();
        Enabled = true;
    }
}