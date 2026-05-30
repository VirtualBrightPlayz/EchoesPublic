using System;

public class FriendlyFireCommand : AdminConsoleCommandButton
{

    public override string Command { get; } = "FriendlyFire";
    public override string[] Alias { get; } = ["ff"];
    public override string CommandDescription { get; } = "Disables or enables ff";
    public override string ButtonLabel => "FF Toggle";
    public override Type Category => typeof(AdminCategoryRound);
    public override string Container => nameof(AdminCategoryRound.Container.Map);
    
    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        Settings.Server.FriendlyFire = !Settings.Server.FriendlyFire;
        response = Settings.Server.FriendlyFire ? "Enabled Friendly Fire" : "Disabled Friendly Fire";
        return true;
    }
}