using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using GodotSteam;

public partial class LobbyListUI : VBoxContainer
{
    public Dictionary<ulong, Control> serverListings = new Dictionary<ulong, Control>();
    public string serverSearch = string.Empty;
    public List<string> requiredTags = new List<string>();
    public List<string> disallowedTags = new List<string>();
    public bool loadingServers = false;
    public NetworkManager.ServerListPost currentServerInfo;
    [Export]
    public Control mainList;
    [Export]
    public Control serverInfo;
    [Export]
    public RichTextLabel infoLabel;

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
        /*
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
        */
    }

    public override void _ExitTree()
    {
        VisibilityChanged -= Refresh;
        infoLabel.MetaClicked -= OpenUrl;
        loadingServers = false;
        // samePlatformOnly.Toggled -= SetPlatformFilter;
    }

    private void OpenUrl(Variant meta)
    {
        if (meta.AsString().StartsWith("http://") || meta.AsString().StartsWith("https://"))
        {
            OS.ShellOpen(meta.AsString());
        }
    }

    public void LoadIcon(TextureRect icon, ulong server)
    {
        string owner = Steam.GetLobbyData(server, "owner");
        ulong ownerId = ulong.Parse(owner);
        SteamManager.Instance.GetAvatar(ownerId, img =>
        {
            if (IsInstanceValid(this) && IsInstanceValid(icon) && IsInstanceValid(img))
            {
                icon.Texture = ImageTexture.CreateFromImage(img);
            }
        });
    }

    public void SetPlatformFilter(bool value)
    {
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
            if (!IsInstanceValid(kvp.Value))
                continue;
            bool val = string.IsNullOrEmpty(serverSearch) || Steam.GetLobbyData(kvp.Key, "name").Contains(serverSearch, StringComparison.CurrentCultureIgnoreCase);
            val &= Steam.GetLobbyData(kvp.Key, "version").Contains(ProjectSettings.GetSetting("application/config/version").ToString());
            kvp.Value.Visible = val;
        }
    }

    public async void ViewInfo(NetworkManager.ServerListPost server)
    {
        mainList.Visible = false;
        serverInfo.Visible = true;
        currentServerInfo = server;
        infoLabel.Text = Tr("LOADING_TEXT");
        if (string.IsNullOrWhiteSpace(server.infoUrl))
        {
            infoLabel.Text = "No server info provided.";
            return;
        }
        try
        {
            string info = Encoding.UTF8.GetString(await HttpUtils.HttpGetAsync(this, server.infoUrl));
            if (string.IsNullOrWhiteSpace(info))
                infoLabel.Text = "Error getting server info. Check console for details.";
            else
                infoLabel.Text = info;
        }
        catch (Exception e)
        {
            Log.PrintErr(e);
            infoLabel.Text = "Error getting server info. Check console for details.";
        }
    }

    public void Join()
    {
        MenuManager.Instance.Join(currentServerInfo.address, currentServerInfo.name);
    }

    public void Back()
    {
        mainList.Visible = true;
        serverInfo.Visible = false;
    }

    public void Refresh()
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
        SteamManager.Instance.PublicLobbyList(servers =>
        {
            if (!IsInstanceValid(this))
                return;
            serverListings.Clear();
            foreach (var server in servers)
            {
                if (serverListings.ContainsKey(server))
                    continue;
                // ulong ownerId = Steam.GetLobbyOwner(server);
                var panel = new PanelContainer();
                var box = new StyleBoxFlat();
                box.BgColor = Colors.Black;
                panel.AddThemeStyleboxOverride("panel", box);
                mainList.AddChild(panel);

                var margin = new MarginContainer();
                int amount = 6;
                margin.AddThemeConstantOverride("margin_left", amount);
                margin.AddThemeConstantOverride("margin_top", amount);
                margin.AddThemeConstantOverride("margin_right", amount);
                margin.AddThemeConstantOverride("margin_bottom", amount);
                panel.AddChild(margin);

                var hbox = new HBoxContainer();
                margin.AddChild(hbox);
                hbox.SetAnchorsPreset(LayoutPreset.FullRect);

                var icon = new TextureRect();
                icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                icon.CustomMinimumSize = Vector2.One * 32f;
                LoadIcon(icon, server);
                hbox.AddChild(icon);

                var label = new RichTextLabel();
                label.BbcodeEnabled = true;
                label.FitContent = true;
                label.ScrollActive = false;
                label.ShortcutKeysEnabled = false;
                hbox.AddChild(label);

                var btnVBox = new VBoxContainer();
                hbox.AddChild(btnVBox);

                // var infoBtn = new Button();
                // btnVBox.AddChild(infoBtn);
                // infoBtn.Text = "PLAY_MENU_INFO_LISTING";
                // infoBtn.Pressed += () => ViewInfo(server);

                var joinBtn = new Button();
                btnVBox.AddChild(joinBtn);
                joinBtn.Text = "PLAY_MENU_JOIN_LISTING";
                joinBtn.Pressed += () => Steam.JoinLobby(server);

                label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                label.Text = $"{Steam.GetLobbyData(server, "name")} ({Steam.GetNumLobbyMembers(server)}/{Steam.GetLobbyMemberLimit(server)})";

                serverListings.Add(server, panel);
            }
            FilterResults();
        }, () =>
        {
            if (!IsInstanceValid(this))
                return;
            loadingServers = false;
        });
    }
}
