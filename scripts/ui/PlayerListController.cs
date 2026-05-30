using System;
using System.Linq;
using Godot;

public partial class PlayerListController : Node
{
    [Export]
    public Control playerListRoot;
    [Export]
    public Control playerListing;
    [Export]
    public RichTextLabel playerListLabel;
    private IPlayerList _playerList;

    public override void _EnterTree()
    {
        _playerList = IPlayerList.List(this);
        _playerList.PlayerJoined += _Joined;
        _playerList.PlayerLeft += _Left;
        playerListRoot.VisibilityChanged += _Refresh;
        _Refresh();
    }

    public override void _ExitTree()
    {
        _playerList.PlayerJoined -= _Joined;
        _playerList.PlayerLeft -= _Left;
        playerListRoot.VisibilityChanged -= _Refresh;
    }

    private void _Refresh()
    {
        foreach (var item in playerListRoot.GetChildren())
        {
            if (int.TryParse(item.Name, out _))
                item.QueueFreeNow();
        }
        foreach (var plr in _playerList.PlayerList)
        {
            _Joined(plr);
        }
    }

    private void _UpdateListing(Control listing, bool create)
    {
        NetworkPlayer player = _playerList.PlayerList.FirstOrDefault(x => x.AuthorityId.ToString() == listing.Name);
        if (!IsInstanceValid(player))
            return;
        Label username = listing.GetNodeOrNull<Label>("Username");
        if (IsInstanceValid(username))
            username.Text = player.username;
        Label role = listing.GetNodeOrNull<Label>("Role");
        if (IsInstanceValid(role))
            role.Text = player.console.IsAdmin ? "Admin" : string.Empty;
        Label ping = listing.GetNodeOrNull<Label>("Ping");
        if (IsInstanceValid(ping))
        {
            if (player.IsLocalPlayer)
            {
                ping.Text = NetworkManager.Instance.PingMS.TryGetValue((int)MultiplayerPeer.TargetPeerServer, out ulong ms) ? ms.ToString() + " ms" : "N/A";
            }
            else
            {
                ping.Text = NetworkManager.Instance.PingMS.TryGetValue(player.AuthorityId, out ulong ms2) ? ms2.ToString() + " ms" : "N/A";
            }
        }
        Godot.Range volume = listing.GetNodeOrNull("Volume") as Godot.Range;
        if (IsInstanceValid(volume))
        {
            if (create)
            {
                volume.SetValueNoSignal(Mathf.DbToLinear(player.voiceChat.VolumeDb) * 100f);
                volume.ValueChanged += v => player.voiceChat.VolumeDb = Mathf.LinearToDb((float)v / 100f);
            }
        }
    }

    private void _Joined(NetworkPlayer player)
    {
        _Left(player);
        Control listing = (Control)playerListing.Duplicate();
        listing.Visible = true;
        listing.Name = player.AuthorityId.ToString();
        playerListRoot.AddChild(listing, true);
        _UpdateListing(listing, true);
    }

    private void _Left(NetworkPlayer player)
    {
        var item = playerListRoot.GetNodeOrNull(player.AuthorityId.ToString());
        if (IsInstanceValid(item))
        {
            playerListRoot.RemoveChild(item);
            item.QueueFreeNow();
        }
    }

    public override void _Process(double delta)
    {
        if (!playerListRoot.IsVisibleInTree())
            return;
        if (playerListLabel != null && RoundManager.Instance != null)
        {
            playerListLabel.Text = RoundManager.Instance.ServerSettings.Name;
        }
        foreach (var item in playerListRoot.GetChildren())
        {
            if (item is Control ctrl)
            {
                _UpdateListing(ctrl, false);
            }
        }
    }
}