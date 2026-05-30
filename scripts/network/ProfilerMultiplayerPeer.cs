using Godot;

public partial class ProfilerMultiplayerPeer : MultiplayerPeerExtension
{
    public const string PlotName = "ReliableOut";
    public const string PlotName1 = "UnReliableOut";
    public const string PlotName2 = "ReliableIn";
    public const string PlotName3 = "UnReliableIn";

    public MultiplayerPeer peer;

    private int inbound = 0;
    private int outbound = 0;

    private int inbound2 = 0;
    private int outbound2 = 0;

    public static ProfilerMultiplayerPeer Wrap(MultiplayerPeer peer)
    {
        return new ProfilerMultiplayerPeer(peer);
    }

    public ProfilerMultiplayerPeer(MultiplayerPeer p)
    {
        peer = p;
        Profiler.PlotConfig(PlotName, Profiler.PlotType.Memory);
        Profiler.PlotConfig(PlotName1, Profiler.PlotType.Memory);
        Profiler.PlotConfig(PlotName2, Profiler.PlotType.Memory);
        Profiler.PlotConfig(PlotName3, Profiler.PlotType.Memory);
        peer.PeerConnected += EmitSignalPeerConnected;
        peer.PeerDisconnected += EmitSignalPeerDisconnected;
    }

    public override void _Close()
    {
        peer.Close();
    }

    public override void _DisconnectPeer(int pPeer, bool pForce)
    {
        peer.DisconnectPeer(pPeer, pForce);
    }

    public override int _GetAvailablePacketCount()
    {
        return peer.GetAvailablePacketCount();
    }

    public override ConnectionStatus _GetConnectionStatus()
    {
        return peer.GetConnectionStatus();
    }

    public override int _GetMaxPacketSize()
    {
        // TODO
        return base._GetMaxPacketSize();
    }

    public override int _GetPacketChannel()
    {
        return peer.GetPacketChannel();
    }

    public override TransferModeEnum _GetPacketMode()
    {
        return peer.GetPacketMode();
    }

    public override int _GetPacketPeer()
    {
        return peer.GetPacketPeer();
    }

    public override byte[] _GetPacketScript()
    {
        byte[] pBuffer = peer.GetPacket();
        if (peer.GetPacketMode() == TransferModeEnum.Reliable)
            inbound += pBuffer.Length;
        else
            inbound2 += pBuffer.Length;
        return pBuffer;
    }

    public override int _GetTransferChannel()
    {
        return peer.GetTransferChannel();
    }

    public override TransferModeEnum _GetTransferMode()
    {
        return peer.GetTransferMode();
    }

    public override int _GetUniqueId()
    {
        return peer.GetUniqueId();
    }

    public override bool _IsRefusingNewConnections()
    {
        return peer.IsRefusingNewConnections();
    }

    public override bool _IsServer()
    {
        return _GetUniqueId() == TargetPeerServer;
    }

    public override bool _IsServerRelaySupported()
    {
        return peer.IsServerRelaySupported();
    }

    public override void _Poll()
    {
        // Profiler.EmitFrameMark();
        Profiler.Plot(PlotName, outbound);
        Profiler.Plot(PlotName1, outbound2);
        Profiler.Plot(PlotName2, inbound);
        Profiler.Plot(PlotName3, inbound2);
        inbound = 0;
        outbound = 0;
        inbound2 = 0;
        outbound2 = 0;
        peer.Poll();
    }

    public override Error _PutPacketScript(byte[] pBuffer)
    {
        if (peer.TransferMode == TransferModeEnum.Reliable)
            outbound += pBuffer.Length;
        else
            outbound2 += pBuffer.Length;
        return peer.PutPacket(pBuffer);
    }

    public override void _SetRefuseNewConnections(bool pEnable)
    {
        peer.SetRefuseNewConnections(pEnable);
    }

    public override void _SetTargetPeer(int pPeer)
    {
        peer.SetTargetPeer(pPeer);
    }

    public override void _SetTransferChannel(int pChannel)
    {
        peer.SetTransferChannel(pChannel);
    }

    public override void _SetTransferMode(TransferModeEnum pMode)
    {
        peer.SetTransferMode(pMode);
    }
}