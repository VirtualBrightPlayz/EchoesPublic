using System;

public class CreditsCommand : SimpleGameCommandBase
{
    public override string Command => "Credits";

    public override string[] Alias { get; } = Array.Empty<string>();

    public override string CommandDescription { get; } = "Prints the game's credits.";

    public override bool Execute(string[] args, out string response)
    {
        response = '\n' + CreditsViewer.GetConsoleCredits();
        return true;
    }
}
