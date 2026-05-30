using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class VoiceListUI : Node
{
    [Export]
    public PackedScene uiScene;
    [Export]
    public Control container;
    public List<VoiceOverlayUI> instances = new List<VoiceOverlayUI>();
    private IPlayerList list;

    public override void _EnterTree()
    {
        list = IPlayerList.List(this);
        list.PlayerJoined += Joined;
        list.PlayerLeft += Left;
        foreach (var player in list.PlayerList)
            Joined(player);
    }

    public override void _ExitTree()
    {
        list.PlayerJoined -= Joined;
        list.PlayerLeft -= Left;
    }

    private void Joined(NetworkPlayer player)
    {
        CallDeferred(nameof(JoinedLate), player);
    }

    private void JoinedLate(NetworkPlayer player)
    {
        var node2 = instances.FirstOrDefault(x => x.player == player);
        if (node2 != null)
            return;
        var node = uiScene.Instantiate<VoiceOverlayUI>();
        node.player = player;
        container.AddChild(node);
        instances.Add(node);
    }

    private void Left(NetworkPlayer player)
    {
        CallDeferred(nameof(LeftLate), player);
    }

    private void LeftLate(NetworkPlayer player)
    {
        var node = instances.FirstOrDefault(x => x.player == player);
        if (node == null)
            return;
        instances.Remove(node);
        node.QueueFree();
    }
}
