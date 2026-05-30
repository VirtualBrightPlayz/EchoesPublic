using System;
using Godot;

public class EndRoundCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "EndRound";
    
    public override string[] Alias { get; } = ["er", "roundend", "stopround", "roundstop"];
    
    public override string CommandDescription => "Ends the round";

    public override string ButtonLabel => "Force Round End";
    
    public override Type Category => typeof(AdminCategoryRound);
    
    public override string Container => nameof(AdminCategoryRound.Container.State);

    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        if (RoundManager.Instance.state != RoundManager.RoundState.InGame)
        {
            response = "No round to end.";
            return false;
        }

        RoundManager.Instance.EndRound();

        response = "Round ended.";
        return true;
    }
}