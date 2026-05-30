using System;
using System.Collections.Generic;
using System.Linq;
using Godot;


// TODO: Make attributes 
public interface IConsoleCommand
{
    string Command { get; }

    string[] Alias { get; }

    string CommandDescription { get; }

    bool Execute(ICommandSender sender, string[] args, out string response);
}

public interface ICommandSender
{
}

// Move this helper to somewhere better since it can be used for literally anything
// Or move this to the NetworkPlayer class or any type of wrapper class
// If this already existed mb I looked into the code and did not find anything
public static class CommandHelper
{
    public static HashSet<NetworkPlayer> ParsePlayersArgument(string unformatedPlayers)
    {
        string[] players = unformatedPlayers.Split(',');
        
        HashSet<NetworkPlayer> playersList = [];

        if (players.Length > 0 && players[0] == "*")
            return IPlayerList.List(ItemManager.Instance).PlayerList.ToHashSet();

        foreach (string p in players)
        {
            if (TryParsePlayer(p, out NetworkPlayer player))
                playersList.Add(player);
        }

        return playersList;
    }

    public static bool TryParsePlayer(string p, out NetworkPlayer player) => (player = ParsePlayer(p)) != null;

    public static bool TryParsePlayerByName(string name, out NetworkPlayer player, bool requireFullMatch = false) => (player = ParsePlayerByName(name, requireFullMatch)) != null;

    public static bool TryParsePlayerById(int id, out NetworkPlayer player) => (player = ParsePlayerById(id)) != null;

    public static NetworkPlayer ParsePlayer(string player)
    {
        if (string.IsNullOrEmpty(player))
            return null;
        
        if (int.TryParse(player, out int id) && TryParsePlayerById(id, out NetworkPlayer p1))
            return p1;

        return TryParsePlayerByName(player, out NetworkPlayer p2, false) ? p2 : null;
    }

    public static NetworkPlayer ParsePlayerById(int id) => IPlayerList.List(ItemManager.Instance).PlayerList.FirstOrDefault(p => p.PlayerId == id);

    public static NetworkPlayer ParsePlayerByName(string name, bool requireFullMatch = false)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        
        foreach (NetworkPlayer player in IPlayerList.List(ItemManager.Instance).PlayerList.OrderBy(p => p.Name))
        {
            string playerName = player.username;
            bool check = requireFullMatch ? playerName.Equals(name, StringComparison.OrdinalIgnoreCase) : playerName.StartsWith(name, StringComparison.OrdinalIgnoreCase);

            if (check)
                return player;
        }

        return null;
    }
}