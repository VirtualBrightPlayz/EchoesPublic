using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PDLogic : Node
{
    public class PDPlayerInfo
    {
        public BasePlayer player;
        public int timesExited = 0;
        public ZoneArea.Zone lastZone;
    }

    public static PDLogic instance;
    [Export]
    public int numExits = 1;
    [Export]
    public int MaxTimesExited = 2;
    public bool hasRandomized = false;
    public List<PDPlayerInfo> playerInfos = new List<PDPlayerInfo>();

    public override void _Ready()
    {
        instance = this;
        RoundManager.Instance.EventOnRoundStart += SetupPD;
    }

    public override void _ExitTree()
    {
        RoundManager.Instance.EventOnRoundStart -= SetupPD;
    }

    public void SetupPD()
    {
        if (!IsMultiplayerAuthority())
            return;
        playerInfos.Clear();
        hasRandomized = false;
        var lst = GetExits();
        foreach (var exit in lst)
        {
            exit.isExit = true;
        }
    }

    public List<PDExit> GetExits()
    {
        var areas = GetTree().GetNodesInGroup("pd_area");
        var lst = new List<PDExit>();
        foreach (var area in areas)
        {
            if (area is PDExit exit)
            {
                lst.Add(exit);
            }
        }
        return lst;
    }

    public void RandomizeExits(ulong seed)
    {
        hasRandomized = true;
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = seed;
        var lst = GetExits();

        foreach (var exit in lst)
        {
            exit.isExit = false;
        }

        for (int i = 0; i < numExits; i++)
        {
            if (lst.Count == 0)
                break;
            var idx = (int)(rng.Randi() % lst.Count);
            var exit = lst[idx];
            lst.RemoveAt(idx);
            exit.isExit = true;
        }
    }

    public void OnEnter(BasePlayer player, ZoneArea.Zone zone)
    {
        if (!playerInfos.Any(x => x.player == player))
        {
            playerInfos.Add(new PDPlayerInfo()
            {
                player = player,
            });
        }
        var info = playerInfos.First(x => x.player == player);
        info.lastZone = zone;
    }

    public void OnExit(BasePlayer player, PDExit chosenExit, ref bool kill, out ZoneArea.Zone zone)
    {
        if (player.Health <= player.MaxHealth * 0.25f)
        {
            playerInfos.RemoveAll(x => x.player == player);
            kill = true;
            zone = ZoneArea.Zone.Unknown;
            return;
        }
        var info = playerInfos.FirstOrDefault(x => x.player == player);
        if (info != null)
        {
            if (info.timesExited >= MaxTimesExited)
            {
                playerInfos.RemoveAll(x => x.player == player);
                kill = true;
                zone = ZoneArea.Zone.Unknown;
                return;
            }
            info.timesExited++;
            zone = info.lastZone;
        }
        else
        {
            zone = ZoneArea.Zone.HeavyContainment;
        }
        if (!hasRandomized)
        {
            hasRandomized = true;
            var lst = GetExits();
            lst.Remove(chosenExit);

            foreach (var exit in lst)
            {
                exit.isExit = false;
            }

            for (int i = 0; i < numExits - 1; i++)
            {
                if (lst.Count == 0)
                    break;
                var idx = (int)(GD.Randi() % lst.Count);
                var exit = lst[idx];
                lst.RemoveAt(idx);
                exit.isExit = true;
            }
        }
    }
}
