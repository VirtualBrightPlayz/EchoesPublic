using System;
using Godot;

public class SpawnWaveCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "SpawnWave";
    
    public override string[] Alias { get; } = new string[] { "spw", "respawnwave", "respawn" };
    
    public override string CommandDescription => "Triggers a respawn wave.";

    public override string ButtonLabel => "Spawn Wave";

    public override Type Category => typeof(AdminCategoryRound);
    
    public override string Container => nameof(AdminCategoryRound.Container.Map);

    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        RoundManager.Instance.Logic.TryRespawnWave(true);
        response = "Wave spawned";
        return true;
    }
}