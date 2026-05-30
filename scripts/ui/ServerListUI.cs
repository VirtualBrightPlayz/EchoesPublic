using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ServerListUI : VBoxContainer
{
    public List<Control> serverListings = new List<Control>();
    public string serverSearch = string.Empty;
    public List<string> requiredTags = new List<string>();
    public List<string> disallowedTags = new List<string>();
    public bool loadingServers = false;
    public IServerListing currentServerInfo;
    [Export]
    public Control mainList;
    [Export]
    public Control serverInfo;
    [Export]
    public RichTextLabel infoLabel;
    [Export]
    public PackedScene serverPrefab;
    [Export]
    public PackedScene lobbyPrefab;

    [ExportGroup("Filter Inputs")]
    [Export]
    public CheckButton samePlatformOnly;

    public override void _Ready()
    {
        requiredTags.Clear();
        disallowedTags.Clear();
        VisibilityChanged += Refresh;
        infoLabel.MetaClicked += OpenUrl;
        loadingServers = false;
        samePlatformOnly.Toggled += SetPlatformFilter;
        if (IInitScript.Instance.IsXR)
        {
            samePlatformOnly.Text = "VR Only";
            disallowedTags.Add(NetworkManager.NoVrOnlyTag);
        }
        else
        {
            samePlatformOnly.Text = "VR Allowed";
            disallowedTags.Add(NetworkManager.VrOnlyTag);
        }
        SetPlatformFilter(samePlatformOnly.ButtonPressed);
    }

    public override void _ExitTree()
    {
        VisibilityChanged -= Refresh;
        infoLabel.MetaClicked -= OpenUrl;
        loadingServers = false;
        samePlatformOnly.Toggled -= SetPlatformFilter;
    }

    private void OpenUrl(Variant meta)
    {
        if (meta.AsString().StartsWith("http://") || meta.AsString().StartsWith("https://"))
        {
            OS.ShellOpen(meta.AsString());
        }
    }

    public async void LoadIcon(TextureRect icon, NetworkManager.ServerListPost server)
    {
        if (string.IsNullOrWhiteSpace(server.iconUrl))
            return;
        try
        {
            byte[] data = await HttpUtils.HttpGetAsync(this, server.iconUrl);
            if (IsInstanceValid(icon))
            {
                Image img = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
                if (img.LoadPngFromBuffer(data) == Error.Ok)
                {
                    icon.Texture = ImageTexture.CreateFromImage(img);
                }
                else if (img.LoadJpgFromBuffer(data) == Error.Ok)
                {
                    icon.Texture = ImageTexture.CreateFromImage(img);
                }
            }
        }
        catch (Exception e)
        {
            Log.PrintErr(e);
        }
    }

    public void SetPlatformFilter(bool value)
    {
        /*
        if (value)
        {
            if (IInitScript.Instance.IsXR)
                requiredTags.Add(NetworkManager.VrOnlyTag);
            else
                requiredTags.Remove(NetworkManager.NoVrOnlyTag);
        }
        else
        {
            if (IInitScript.Instance.IsXR)
                requiredTags.Remove(NetworkManager.VrOnlyTag);
            else
                requiredTags.Add(NetworkManager.NoVrOnlyTag);
        }
        */
        FilterResults();
    }

    public void SetSearch(string searchText)
    {
        serverSearch = searchText;
        FilterResults();
    }

    public void FilterResults()
    {
        foreach (var kvp in serverListings)
        {
            if (!IsInstanceValid(kvp))
                continue;
            var listing = (IServerListing)kvp;
            bool val = string.IsNullOrEmpty(serverSearch) || listing.ServerName.Contains(serverSearch, StringComparison.CurrentCultureIgnoreCase);
            string[] tags = listing.ServerTags;
            if (requiredTags.Count != 0)
                val &= tags.All(requiredTags.Contains);
            if (disallowedTags.Count != 0)
                val &= !tags.All(disallowedTags.Contains);
            val &= tags.Contains(ProjectSettings.GetSetting("application/config/version").ToString());
            kvp.Visible = val;
        }
    }

    public void StartViewInfo(IServerListing listing)
    {
        mainList.Visible = false;
        serverInfo.Visible = true;
        currentServerInfo = listing;
        infoLabel.Text = Tr("LOADING_TEXT");
    }

    public void EndViewInfo(IServerListing listing, string info)
    {
        if (currentServerInfo != listing)
        {
            return;
        }
        mainList.Visible = false;
        serverInfo.Visible = true;
        if (string.IsNullOrWhiteSpace(info))
            infoLabel.Text = "No server info provided.";
        else
            infoLabel.Text = info;
    }

    public void Join()
    {
        if (IsInstanceValid((GodotObject)currentServerInfo))
        {
            currentServerInfo.ServerJoin();
        }
    }

    public void Back()
    {
        mainList.Visible = true;
        serverInfo.Visible = false;
    }

    public async Task<ulong[]> RefreshLobbyList()
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        List<ulong> ids = new List<ulong>();
        SteamManager.Instance.PublicLobbyList(ids.AddRange, tcs.SetResult);
        await tcs.Task;
        return ids.ToArray();
    }

    public async Task<ulong[]> RefreshFriendLobbyList()
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        List<ulong> ids = new List<ulong>();
        SteamManager.Instance.FriendLobbyList(ids.AddRange, tcs.SetResult);
        await tcs.Task;
        return ids.ToArray();
    }

    public async Task<NetworkManager.ServerListPost[]> RefreshServerList()
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        List<NetworkManager.ServerListPost> ids = new List<NetworkManager.ServerListPost>();
        NetworkManager.Instance.PublicServerList(ids.AddRange, tcs.SetResult);
        await tcs.Task;
        return ids.ToArray();
    }

    public void Refresh()
    {
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (loadingServers)
            return;
        loadingServers = true;
        mainList.Visible = true;
        serverInfo.Visible = false;
        foreach (var ch in mainList.GetChildren())
            ch.QueueFree();
        serverListings.Clear();
        if (!Visible)
            return;
        var servers = await RefreshServerList();
        foreach (var sv in servers)
        {
            var ui = serverPrefab.Instantiate<ServerListingUI>();
            ui.ServerList = this;
            ui.ListInfo = sv;
            ui.Visible = false;
            mainList.AddChild(ui);
            serverListings.Add(ui);
        }
        FilterResults();
        var lobbies = await RefreshLobbyList();
        foreach (var sv in lobbies)
        {
            var ui = lobbyPrefab.Instantiate<LobbyListingUI>();
            ui.ServerList = this;
            ui.Setup(sv);
            ui.Visible = false;
            mainList.AddChild(ui);
            serverListings.Add(ui);
        }
        FilterResults();
        var friends = await RefreshFriendLobbyList();
        foreach (var sv in friends.Distinct())
        {
            if (lobbies.Contains(sv))
            {
                continue;
            }
            var ui = lobbyPrefab.Instantiate<LobbyListingUI>();
            ui.ServerList = this;
            ui.Setup(sv);
            ui.Visible = false;
            mainList.AddChild(ui);
            serverListings.Add(ui);
        }
        FilterResults();
        loadingServers = false;
    }
}
