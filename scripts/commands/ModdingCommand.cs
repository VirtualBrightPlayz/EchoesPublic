using System;
using Godot;

public class ModdingCommand : SimpleGameCommandBase
{
    public override string Command { get; } = "Modding";

    public override string[] Alias { get; } = ["mod", "ugc"];

    public override string CommandDescription { get; } = "Modding related utility command.";

    public override bool Execute(string[] args, out string response)
    {
        if (args.Length > 0)
        {
            if (args[0].Equals("editor", StringComparison.InvariantCultureIgnoreCase))
            {
                NetworkManager.Instance.Shutdown();
                MenuManager.Instance.LoadWorkshopEditor();
                response = "Opening the workshop editor...";
                return true;
            }
            else if (args[0].Equals("reload", StringComparison.InvariantCultureIgnoreCase))
            {
                MenuManager.Instance.Data.Reload();
                response = "Toml file data has been reloaded.";
                return true;
            }
            else if (args[0].Equals("download", StringComparison.InvariantCultureIgnoreCase))
            {
                SteamManager.Instance.RefreshWorkshopContent();
                response = "Workshop content is being downloaded.";
                return true;
            }
        }
        response = "Invalid sub-command. (editor|reload|download)";
        return false;
    }
}
