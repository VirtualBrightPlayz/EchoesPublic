using System;
using Godot;

public class NukeCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "Nuke";

    public override string[] Alias { get; } = Array.Empty<string>();

    public override string CommandDescription => "Controls the nuke";

    public override string ButtonLabel => "Toggle Nuke";

    public override Type Category => typeof(AdminCategoryRound);
    
    public override string Container => nameof(AdminCategoryRound.Container.Map);

    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        RoundManager.Instance.NukeActive = !RoundManager.Instance.NukeActive;
        response = RoundManager.Instance.NukeActive ? "Nuke Enabled" : "Nuke Disabled";
        return true;
    }
}