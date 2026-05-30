using Godot;
using GodotSteam;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class SteamManager : Node
{
    public static bool Supported => OS.HasFeature("steam") && !IInitScript.IsHeadless;
    public static SteamManager Instance => MenuManager.Instance?.statusManager?.steam; // macro ahh
    [Export]
    public string AppId = "480";
    [Export]
    public string AppIdPlaytest = "480";

    public ulong LobbyId = 0;
    public string LobbyName = string.Empty;

    public Dictionary<long, string> SubscribedItems = new Dictionary<long, string>();

    private Action<ulong[]> onServerListings;
    private Action onDone;
    private Dictionary<ulong, Action<Image>> avatarLoadCallbacks = new Dictionary<ulong, Action<Image>>();
    private Dictionary<ulong, Image> avatarCache = new Dictionary<ulong, Image>();
    private List<ulong> friendLobbies = new List<ulong>();
    private int friendCount = 0;

    public override void _EnterTree()
    {
        if (!Supported)
            return;
        string appid = OS.HasFeature("playtest") ? AppIdPlaytest : AppId;
        OS.SetEnvironment("SteamAppId", appid);
        OS.SetEnvironment("SteamGameId", appid);
        SteamInitExResult result = Steam.SteamInitEx(true, (uint)appid.ToInt());
        Steam.InitRelayNetworkAccess();
        if (result.Status == SteamInitExStatus.SteamworksActive)
        {
        }
        else if (!IInitScript.IsServerOnly)
        {
            OS.Alert(result.Verbal);
        }
    }

    public override void _Ready()
    {
        if (!Supported)
            return;
        Steam.LobbyCreated += _OnLobbyCreated;
        Steam.LobbyJoined += _OnLobbyJoined;
        Steam.JoinRequested += _OnJoinRequested;
        Steam.LobbyMatchList += _OnLobbyList;
        Steam.AvatarLoaded += _OnAvatar;
        Steam.PersonaStateChange += _OnPersonaState;
        Steam.LobbyDataUpdate += _OnLobbyData;
        Steam.ItemInstalled += _OnItemInstalled;
        Steam.ItemDownloaded += _OnItemDownloaded;
        RefreshWorkshopContent();
    }

    public override void _Process(double delta)
    {
        if (!Supported)
            return;
        Steam.RunCallbacks();
    }

    private void _OnItemDownloaded(long result, long fileId, uint appId)
    {
        if (appId == Steam.GetAppID())
        {
            if (result != (long)ErrorResult.Ok)
            {
                Log.PrintErr($"Error downloading Workshop item {fileId} with error: ({(ErrorResult)result})");
                return;
            }
        }
    }

    private void _OnItemInstalled(uint appId, ulong fileId, ulong legacyContent, ulong manifestId)
    {
        if (appId == Steam.GetAppID())
        {
            RefreshWorkshopContent();
            IInitScript.Instance.Data.Reload();
        }
    }

    public void RefreshWorkshopContent()
    {
        if (!Supported)
            return;
        SubscribedItems.Clear();
        foreach (var item in Steam.GetSubscribedItems())
        {
            Steam.ItemState state = (Steam.ItemState)Steam.GetItemState(item.AsInt64());
            Log.Print($"{item.AsInt64()} {state}");
            if (state == Steam.ItemState.None || state.HasFlag(Steam.ItemState.NeedsUpdate))
            {
                Steam.DownloadItem(item.AsUInt64(), false);
            }
            else if (!state.HasFlag(Steam.ItemState.Installed))
            {
                continue;
            }
            var info = Steam.GetItemInstallInfo(item.AsInt64());
            if (!info["ret"].AsBool())
            {
                Log.PrintWarn($"Workshop item {item.AsInt64()} not installed or has no content.");
                continue;
            }
            SubscribedItems.Add(item.AsInt64(), info["folder"].AsString());
        }
    }

    public void InstallWorkshopItem(long id)
    {
        if (!Supported)
            return;
        Steam.SubscribeItem(id);
        RefreshWorkshopContent();
    }

    public void PublicLobbyList(Action<ulong[]> onServerListings, Action onDone)
    {
        if (!Supported)
        {
            onDone?.Invoke();
            return;
        }
        this.onServerListings = onServerListings;
        this.onDone = onDone;
        Steam.AddRequestLobbyListDistanceFilter(Steam.LobbyDistanceFilter.Worldwide);
        Steam.RequestLobbyList();
    }

    public void FriendLobbyList(Action<ulong[]> onServerListings, Action onDone)
    {
        if (!Supported)
        {
            onDone?.Invoke();
            return;
        }
        this.onServerListings = onServerListings;
        this.onDone = onDone;
        friendLobbies.Clear();
        int count = Steam.GetFriendCount(FriendFlag.Immediate);
        friendCount = 0;
        friendLobbies.EnsureCapacity(count);
        for (int i = 0; i < count; i++)
        {
            ulong steamId = Steam.GetFriendByIndex(i, FriendFlag.Immediate);
            var dict = Steam.GetFriendGamePlayed(steamId);
            if (!dict.HasValue)
                continue;
            if (Steam.GetAppID() != dict.Value.Id)
                continue;
            var lobby = dict.Value.Lobby;
            if (!lobby.HasValue)
                continue;
            friendCount++;
            if (!Steam.RequestLobbyData(lobby.Value))
            {
                friendLobbies.Add(lobby.Value);
            }
        }
        if (friendLobbies.Count >= friendCount)
        {
            onServerListings?.Invoke(friendLobbies.ToArray());
            onDone?.Invoke();
        }
    }

    public void GetAvatar(ulong steamId, Action<Image> callback)
    {
        if (!Supported)
        {
            callback?.Invoke(null);
            return;
        }
        if (avatarCache.TryGetValue(steamId, out var img))
        {
            callback?.Invoke(img);
            return;
        }
        avatarLoadCallbacks[steamId] = callback;
        if (!Steam.RequestUserInformation(steamId, false))
        {
            Steam.GetPlayerAvatar(AvatarSize.Large, steamId);
        }
    }

    private void _OnLobbyData(uint success, ulong lobbyID, ulong memberID)
    {
        friendLobbies.Add(lobbyID);
        if (friendLobbies.Count >= friendCount)
        {
            onServerListings?.Invoke(friendLobbies.ToArray());
            onDone?.Invoke();
        }
    }

    private void _OnPersonaState(ulong steamId, PersonaChange flags)
    {
        if (flags.HasFlag(PersonaChange.Avatar) && avatarLoadCallbacks.TryGetValue(steamId, out var cb))
        {
            Steam.GetPlayerAvatar(AvatarSize.Large, steamId);
        }
    }

    private void _OnAvatar(ulong avatarId, int width, byte[] data)
    {
        var img = Image.CreateFromData(width, width, false, Image.Format.Rgba8, data);
        avatarCache[avatarId] = img;
        if (avatarLoadCallbacks.TryGetValue(avatarId, out var cb))
        {
            cb?.Invoke(img);
        }
        avatarLoadCallbacks.Remove(avatarId);
    }

    private void _OnLobbyList(Godot.Collections.Array lobbies)
    {
        List<ulong> lobbyIds = new List<ulong>();
        foreach (var lobby in lobbies)
        {
            ulong lobbyId = lobby.AsUInt64();
            lobbyIds.Add(lobbyId);
        }
        onServerListings?.Invoke(lobbyIds.ToArray());
        onDone?.Invoke();
    }

    private void _OnJoinRequested(ulong lobbyId, ulong steamId)
    {
        DestroyLobby();
        Log.PrintInfo($"Joining player {steamId}");
        Steam.JoinLobby(lobbyId);
    }

    private void _OnLobbyJoined(ulong lobby, long permissions, bool locked, long response)
    {
        Log.PrintInfo($"Joining lobby {lobby} ({response})");
        if (response == 1) // k_EChatRoomEnterResponseSuccess
        {
            if (LobbyId == lobby)
            {
                return;
            }
            LobbyId = lobby;
            MenuManager.Instance.JoinSteamId(lobby);
        }
    }

    private void _OnLobbyCreated(long connect, ulong lobbyId)
    {
        Log.PrintInfo($"Lobby created {lobbyId}");
        if (connect == 1)
        {
            LobbyId = lobbyId;
            Steam.SetLobbyJoinable(LobbyId, true);
            Steam.SetLobbyData(LobbyId, "name", LobbyName);
            Steam.SetLobbyData(LobbyId, "owner", Steam.GetSteamID().ToString());
            Steam.SetLobbyData(LobbyId, "version", ProjectSettings.GetSetting("application/config/version").ToString());
            NetworkManager.Instance.OnSteamLobbyCreated(lobbyId);
        }
    }

    public void CreateLobby(Steam.LobbyType type, int maxClients, string name)
    {
        if (!Supported)
            return;
        if (LobbyId == 0)
        {
            LobbyName = name;
            Steam.CreateLobby(type, maxClients);
        }
    }

    public void JoinLobby(ulong lobbyId)
    {
        if (!Supported)
            return;
        Steam.JoinLobby(lobbyId);
    }

    public void DestroyLobby()
    {
        if (!Supported)
            return;
        if (LobbyId != 0)
        {
            Steam.LeaveLobby(LobbyId);
            LobbyId = 0;
            LobbyName = string.Empty;
        }
    }

    public bool LobbyHasSteamId(ulong id)
    {
        if (!Supported)
            return false;
        if (LobbyId == 0)
            return false;
        // Log.Print(Steam.GetNumLobbyMembers(LobbyId));
        int len = Steam.GetNumLobbyMembers(LobbyId);
        for (int i = 0; i < len; i++)
        {
            Log.Print(Steam.GetLobbyMemberByIndex(LobbyId, i));
            if (Steam.GetLobbyMemberByIndex(LobbyId, i) == id)
                return true;
        }
        return false;
    }

    public void SetMenuActivity()
    {
        if (!Supported)
            return;
        Steam.SetRichPresence("status", "On the main menu");
        Steam.SetRichPresence("steam_display", "#Generic");
        Steam.SetRichPresence("steam_player_group", string.Empty);
        Steam.SetRichPresence("steam_player_group_size", string.Empty);
        Steam.Instance.Call("setTimelineGameMode", 3); // 3 = menu
        Steam.Instance.Call("clearTimelineTooltip", 0f);
    }

	public void SetActivityRole(PlayerRole role, bool setTime = false, string serverName = "", string serverImage = "")
    {
        if (!Supported)
            return;
        string serverNameFinal = string.Empty;
        int tagDepth = 0;
        if (serverName == null)
        {
            serverName = string.Empty;
        }
        for (int i = 0; i < serverName.Length; i++)
        {
            if (serverName[i] == '[')
                tagDepth++;
            else if (serverName[i] == ']')
                tagDepth--;
            else if (tagDepth == 0)
            {
                serverNameFinal += serverName[i];
            }
        }
        Steam.SetRichPresence("status", $"Somewhere in {serverNameFinal}");
        // Steam.SetRichPresence("connect", NetworkManager.Instance.GetPeerAddressAndPort(1));
        Steam.SetRichPresence("steam_display", "#Generic");
        if (LobbyId == 0)
        {
            Steam.SetRichPresence("steam_player_group", string.Empty);
            Steam.SetRichPresence("steam_player_group_size", string.Empty);
        }
        else
        {
            Steam.SetRichPresence("steam_player_group", LobbyId.ToString());
            Steam.SetRichPresence("steam_player_group_size", Steam.GetNumLobbyMembers(LobbyId).ToString());
        }
        if (role.team == TeamID.Dead)
        {
            Steam.Instance.Call("setTimelineGameMode", 2); // 2 = staging
        }
        else
        {
            Steam.Instance.Call("setTimelineGameMode", 1); // 1 = in game
        }
        Steam.Instance.Call("setTimelineTooltip", "Playing as a " + role.PlainDisplayName, 0f);
    }

    public void EndRound()
    {
        if (!Supported)
            return;
        Steam.Instance.Call("addInstantaneousTimelineEvent", "Round Ended", "Round Ended", "steam_ribbon", 0, 0f, 1);
    }

    public void PlayerKilled(BasePlayer otherPlayer)
    {
        if (!Supported)
            return;
        Steam.Instance.Call("addInstantaneousTimelineEvent", "Killed a Player", $"Killed {otherPlayer.AttackerDisplayName}", "steam_attack", 0, 0f, 1);
    }

    public void EndChase(float length)
    {
        if (!Supported)
            return;
        Steam.Instance.Call("addRangeTimelineEvent", "Spotted SCP", "", "steam_view", 0, -length, length, 1);
    }
}
