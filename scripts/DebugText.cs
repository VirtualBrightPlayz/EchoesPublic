using Godot;
using GodotSteam;
using System;

public partial class DebugText : Label
{
    private Rid rid;

    [Export]
    public bool HideDebugInfo { get; set; } = false;

    public Node Hider { get; set; } = null;
    
    public static DebugText Instance;
    
    public override void _EnterTree()
    {
        rid = GetViewport().GetViewportRid();
        if (Instance != null && Instance != this)
        {
            Log.PrintErr("Multiple instances of debug text!");
            QueueFree();
            return;
        }
        Instance = this;
        // RenderingServer.ViewportSetMeasureRenderTime(rid, true);
    }

    public override void _ExitTree()
    {
        // RenderingServer.ViewportSetMeasureRenderTime(rid, false);
    }

    public override void _Process(double delta)
    {
        if (HideDebugInfo)
        {
            if (!IsInstanceValid(Hider))
            {
                Visible = true;
            }
            else
            {
                Visible = false;
            }
        }
        else
        {
            Visible = true;
        }
        // double cpu = RenderingServer.ViewportGetMeasuredRenderTimeCpu(rid);
        // double gpu = RenderingServer.ViewportGetMeasuredRenderTimeGpu(rid);
        // int drawCalls = RenderingServer.ViewportGetRenderInfo(rid, RenderingServer.ViewportRenderInfoType.Visible, RenderingServer.ViewportRenderInfo.DrawCallsInFrame);
        ulong? ping = null;
        // double? ping = null;
        double? loss = null;
        string version = $"v{ProjectSettings.GetSetting("application/config/version")}";
        if (SteamManager.Supported)
        {
            version += $"\nBuild ID: {Steam.GetAppBuildId()}";
        }
        if (NetworkManager.Instance.Multiplayer.HasMultiplayerPeer())
        {
            if (NetworkManager.Instance.PingMS.TryGetValue((int)MultiplayerPeer.TargetPeerServer, out ulong p))
            {
                ping = p;
                Text = $"{(ping ?? 0):0} ms|{Engine.GetFramesPerSecond()} FPS\n{version}";
            }
        }
        if (NetworkManager.Instance.Multiplayer.HasMultiplayerPeer() && NetworkManager.Instance.Multiplayer.MultiplayerPeer is ENetMultiplayerPeer peer && peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected && !NetworkManager.Instance.Multiplayer.IsServer() && IsInstanceValid(peer.Host))
        {
            foreach (var pp in peer.Host.GetPeers())
            {
                ping = (ulong)pp.GetStatistic(ENetPacketPeer.PeerStatistic.LastRoundTripTime);
                loss = pp.GetStatistic(ENetPacketPeer.PeerStatistic.PacketLoss);
                break;
            }
            Text = $"{(ping ?? 0):0} ms|{(loss ?? 0):0} loss|{Engine.GetFramesPerSecond()} FPS\n{version}";
        }
        else
        {
            Text = $"{Engine.GetFramesPerSecond()} FPS\n{version}";
        }
    }
}
