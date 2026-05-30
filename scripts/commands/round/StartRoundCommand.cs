using System;
using Godot;

public class StartRoundCommand : AdminConsoleCommandButton
{
    public override string Command => "StartRound";
    
    public override string[] Alias { get; } = ["roundstart", "startround", "roundbegin"]; // not gonna type sr cause it might be used in the future for sotf restart
    
    public override string CommandDescription => "Starts the round.";

    public override string ButtonLabel => "Force Round Start";

    public override Type Category => typeof(AdminCategoryRound);
    
    public override string Container => nameof(AdminCategoryRound.Container.State);

    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        if (RoundManager.Instance.state != RoundManager.RoundState.WaitingForPlayers)
        {
            response = "No round to start.";
            return false;
        }
        
        RoundManager.Instance.StartRound();

        response = "Round started.";
        return true;
    }
}