using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
[Tool]
public partial class AmbientEnvCtrl : Node
{
    public static List<AmbientEnvZone> zones = new List<AmbientEnvZone>();
    private List<AmbientEnv.EnvData> selectedZones = new List<AmbientEnv.EnvData>();

    public List<AmbientEnvZone> zones2 => GetTree().GetNodesInGroup("ambient_env").Where(x => x is AmbientEnvZone).Select(x => x as AmbientEnvZone).ToList();

    [Export]
    public WorldEnvironment worldEnv;

    public override void _Process(double delta)
    {
        selectedZones.Clear();
        foreach (var zone in zones2)
        {
            float amount = zone.GetAmount();
            if (amount <= 0f)
                continue;
            selectedZones.Add(new AmbientEnv.EnvData(zone.env, amount, zone.priority));
        }
        for (int i = 0; i < selectedZones.Count; i++)
        {
            var zone = selectedZones[i];
            // zone.amount /= selectedZones.Count;
            selectedZones[i] = zone;
        }
        if (IsInstanceValid(worldEnv))
            AmbientEnv.ComputeFog(worldEnv.Environment, selectedZones.OrderBy(x => x.priority).ToArray());
    }
}