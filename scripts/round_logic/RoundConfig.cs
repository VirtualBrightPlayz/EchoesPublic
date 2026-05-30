using Godot;
using Godot.Collections;

public class RoundConfig
{
    public enum QueueMode : byte
    {
        RoundRobinRepeating = 0,
        RandomRepeating = 1,
        RoundRobin = 2,
        Random = 3,
    }

    public int RespawnWaves { get; set; } = -1;
    public double RespawnTime { get; set; } = 180d;
    public double ForceNukeTime { get; set; } = 0d;
    public double LczDeconTime { get; set; } = 0d;
    public double HczDeconTime { get; set; } = 0d;
    public double ControlPointCaptureTime { get; set; } = 90d;
    public int RequiredControlPoints { get; set; } = 2;
    public int CiRespawnChance { get; set; } = 50;
    public bool NoCi { get; set; } = false;
    public QueueMode SpawnQueueMode { get; set; } = QueueMode.RoundRobinRepeating;
    public Array<TeamID> SpawnQueue { get; set; } = new() { TeamID.ClassD, TeamID.SCP, TeamID.NTF, TeamID.ClassD, TeamID.NTF };
    public RoleID FallbackRole { get; set; } = RoleID.ClassD;
    public bool NukeOnNoScps { get; set; } = false;
    public bool DisableNuke { get; set; } = false;
    public double BackupCalledTimeAdded { get; set; } = -60d;
}
