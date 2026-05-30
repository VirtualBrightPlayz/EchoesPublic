using System;
using System.Linq;
using Godot;

public class RoundPauseCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "RoundPause";

    public override string[] Alias { get; } = new string[] { "rp", "pauseround" };

    public override string CommandDescription { get; } = "Pauses the round.";

    public override string ButtonLabel => "Toggle Round Pause";

    public override Type Category => typeof(AdminCategoryRound);
    
    public override string Container => nameof(AdminCategoryRound.Container.State);

    public override void OnPressed(AdminHUD admin)
    {
        admin.RunCommand($"{Command}");
    }

    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        var instance = RoundManager.Instance;

        if (args.Any())
        {
            switch (args[0])
            {
                case "on":
                case "true":
                case "1":
                    if (instance.Paused)
                    {
                        response = "Round already paused.";
                        return false;
                    }

                    instance.Paused = true;
                    response = "Round paused.";
                    return true;
                case "off":
                case "false":
                case "0":
                    if (!instance.Paused)
                    {
                        response = "Round already unpaused.";
                        return false;
                    }

                    instance.Paused = false;
                    response = "Round unpaused.";
                    return true;
            }
        }

        instance.Paused = !instance.Paused;
        response = $"Round {(instance.Paused ? "paused" : "unpaused")}.";
        return true;
    }
}