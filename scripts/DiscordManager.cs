using Godot;
using System;

[GlobalClass]
public partial class DiscordManager : Node
{
	private GDScript _discordManagerScript;
	private GodotObject _discordManagerGD;
    public static bool Supported => (OS.GetName().Equals("Windows") || OS.GetName().Equals("Linux")) && !IInitScript.IsHeadless;

	public override void _Ready()
	{
        if (!Supported)
            return;
		_discordManagerScript = GD.Load<GDScript>("res://scripts/discord_manager.gd");
		_discordManagerGD = (GodotObject)_discordManagerScript.New();
		AddChild(_discordManagerGD as Node);
    }

    public void SetMenuActivity()
    {
        if (!Supported)
            return;
        _discordManagerGD.Call("set_activity_main_menu");
    }

	public void SetActivityRole(PlayerRole role, bool setTime = false, string serverName = "", string serverImage = "")
    {
        if (!Supported)
            return;
        string serverNameFinal = string.Empty;
        int tagDepth = 0;
        if (serverName == null)
        {
            serverName = string.Empty;
        }
        for (int i = 0; i < serverName.Length; i++)
        {
            if (serverName[i] == '[')
                tagDepth++;
            else if (serverName[i] == ']')
                tagDepth--;
            else if (tagDepth == 0)
            {
                serverNameFinal += serverName[i];
            }
        }
        _discordManagerGD.Call("set_activity_role", role, setTime, serverNameFinal, serverImage);
    }

    public void SetLevelEditorActivity()
    {
        if (!Supported)
            return;
        _discordManagerGD.Call("set_activity_level_editor");
    }
}
