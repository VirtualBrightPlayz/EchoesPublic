using Godot;

[GlobalClass]
public partial class StatusRpcManager : Node
{
    public static StatusRpcManager Instance => MenuManager.Instance?.statusManager; // macro ahh 

    [Export]
    public DiscordManager discord;
    [Export]
    public SteamManager steam;

    public void SetMenuActivity()
    {
        steam.SetMenuActivity();
        discord.SetMenuActivity();
    }

	public void SetActivityRole(PlayerRole role, bool setTime = false, string serverName = "", string serverImage = "")
    {
        steam.SetActivityRole(role, setTime, serverName, serverImage);
        discord.SetActivityRole(role, setTime, serverName, serverImage);
    }

    public void SetLevelEditorActivity()
    {
        // todo: steam activity
        discord.SetLevelEditorActivity();
    }

    public void PlayerKilled(BasePlayer otherPlayer)
    {
        steam.PlayerKilled(otherPlayer);
    }

    public void EndChase(float length)
    {
        steam.EndChase(length);
    }
}