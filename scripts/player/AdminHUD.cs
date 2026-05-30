using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

public partial class AdminHUD : Control
{
    public static StringName META_NAME = "admin_gui";

    [Export]
    public NetworkPlayer Player;

    [Export]
    public ItemList PlayerListGui;
    [Export]
    public ItemList ActionListGui;
    [Export]
    public Control ActionOpsGui;
    [Export]
    public RichTextLabel ResponseLabel;

    [Export]
    public PlayerConsole Console;

    private IPlayerList _playerList;

    private bool _toggled;

    public string CmdPrefix = string.Empty;

    [Export]
    public string[] categoryKeys = Array.Empty<string>();
    [Export]
    public PackedScene[] categoryScenes = Array.Empty<PackedScene>();

    private Control _activeAdminCategory;
    
    private ButtonInputFlags buttonAdminMenu = ButtonInputFlags.None;

    public override void _Ready()
    {
        _playerList = IPlayerList.List(this);

        _toggled = false;
        Visible = false;
        if (Multiplayer.GetUniqueId() != 1)
            CmdPrefix = "/";

        Console.OnCommandResponse += OnResponse;
        OnResponse("", true);

        _playerList.PlayerJoined += OnPlayerJoin;
        _playerList.PlayerLeft += OnPlayerLeave;

        // Add initial players.
        foreach (var player in _playerList.PlayerList)
        {
            PlayerListGui.AddItem("[" + player.AuthorityId + "] " + player.username);
        }

        ActionListGui.Clear();
        foreach (var key in categoryKeys)
        {
            ActionListGui.AddItem(key);
        }

        ActionListGui.ItemSelected += OnActionSelected;
        ActionListGui.Select(0);
        OnActionSelected(0);
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_activeAdminCategory))
        {
            _activeAdminCategory.QueueFree();
        }
        Console.OnCommandResponse -= OnResponse;
        _playerList.PlayerJoined -= OnPlayerJoin;
        _playerList.PlayerLeft -= OnPlayerLeave;
        ActionListGui.ItemSelected -= OnActionSelected;
    }

    public override void _Process(double delta)
    {
        if (!Player.IsLocalPlayer)
            return;
        InputManager.UpdateInput(Player, LocalPlayerInput.AdminMenuName, ref buttonAdminMenu);
        if (buttonAdminMenu.HasFlag(ButtonInputFlags.JustPressed) && Console.IsAdmin && IsMultiplayerAuthority())
        {
            Player.ChangeActionSet(Visible ? "InGame" : "AdminMenu");
        }
        Visible = InputManager.Instance.CurrentSet.ResourceName == "AdminMenu" && IsMultiplayerAuthority();

        if (PlayerListGui.ItemCount != _playerList.PlayerList.Count)
        {
            PlayerListGui.Clear();
            foreach (var plr in _playerList.PlayerList)
            {
                OnPlayerJoin(plr);
            }
        }

        //Update in case names have changed.
        for (int i = 0; i < PlayerListGui.ItemCount; i++)
        {
            var player = _playerList.PlayerList[i];
            PlayerListGui.SetItemText(i, "[" + player.PlayerId + "] " + player.username);
            PlayerListGui.SetItemCustomFgColor(i, player.Role.RoleColor);
        }
    }

    private void OnResponse(string text, bool failed)
    {
        ResponseLabel.Text = $"Output Code: {(failed ? "[color=red]failure[/color]" : "[color=green]success[/color]")}\n{text}";
    }

    private void OnActionSelected(long index)
    {
        if (IsInstanceValid(_activeAdminCategory))
        {
            _activeAdminCategory.QueueFree();
        }
        if (index >= 0 && index < categoryScenes.Length)
        {
            _activeAdminCategory = categoryScenes[index].Instantiate<Control>();
            _activeAdminCategory.SetMeta(META_NAME, this);
            ActionOpsGui.AddChild(_activeAdminCategory);
        }
    }

    public void OnPlayerJoin(NetworkPlayer player)
    {
        // This is handled above (and needs to be changed to be made better).
        int result = PlayerListGui.AddItem("[" + player.PlayerId + "] " + player.username);
        PlayerListGui.SetItemCustomFgColor(result, player.Role.RoleColor);
    }

    public void OnPlayerLeave(NetworkPlayer player)
    {
        // Remove from the list, then readjust the selected index to fix it.
        // If there is no selection, simply remove from the list.
        var playerListIdx = ((List<NetworkPlayer>)_playerList.PlayerList).IndexOf(player);

        if (!PlayerListGui.IsAnythingSelected())
        {
            PlayerListGui.RemoveItem(playerListIdx);
            return;
        }

        var selectedIdx = PlayerListGui.GetSelectedItems()[0];
        PlayerListGui.RemoveItem(playerListIdx);

        if (selectedIdx > playerListIdx)
        {
            // Move back 1 due to the removal.
            PlayerListGui.Select(selectedIdx - 1);
        }
        else if (selectedIdx == playerListIdx)
        {
            // The player is gone, so unselect.
            PlayerListGui.DeselectAll();
        }
    }

    public T GetActiveCategory<T>() where T : Node
    {
        return _activeAdminCategory.GetChildren().OfType<T>().FirstOrDefault();
    }

    public void RunWithSelectedPlayers(string cmd)
    {
        var playerIds = PlayerListGui.GetSelectedItems().Select(id => _playerList.PlayerList[id].PlayerId);
        string selectedPlayerIds = string.Join(',', playerIds);
        Console.ConsoleSubmitted(CmdPrefix + string.Format(cmd, selectedPlayerIds));
    }

    public void RunCommand(string cmd)
    {
        Console.ConsoleSubmitted(CmdPrefix + cmd);
    }
}