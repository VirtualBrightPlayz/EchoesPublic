using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;

public partial class CustomPeer : MultiplayerPeerExtension
{
    public struct Packet
    {
        public int from;
        public byte[] data;
    }

    public struct MetaPacket
    {
        public string msgId;
        public string roomId;
        public string roomName;
        public int isPublic;
        public int metaType;
        public string sessionId;
        public int peerId;
        public string data64;

        public static MetaPacket Create(string roomId, string roomName, bool pub)
        {
            return new MetaPacket()
            {
                msgId = "create",
                roomId = roomId,
                roomName = roomName,
                isPublic = pub ? 1 : 0,
            };
        }

        public static MetaPacket Join(string roomId)
        {
            return new MetaPacket()
            {
                msgId = "join",
                roomId = roomId,
            };
        }

        public static MetaPacket Leave()
        {
            return new MetaPacket()
            {
                msgId = "leave",
            };
        }

        public static MetaPacket MetaSetId(string sessionId, int peerId)
        {
            return new MetaPacket()
            {
                msgId = "meta",
                metaType = 1,
                sessionId = sessionId,
                peerId = peerId,
            };
        }

        public static MetaPacket Rpc(string sessionId, byte[] data)
        {
            return new MetaPacket()
            {
                msgId = "rpc",
                sessionId = sessionId,
                data64 = Convert.ToBase64String(data),
            };
        }

        public static MetaPacket FromString(string str)
        {
            return JsonSerializer.Deserialize<MetaPacket>(str, options);
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, options);
        }
    }

    public partial class User : RefCounted
    {
        public int peerId = 0;
        public string sessionId = string.Empty;
    }

    public enum TargetStateEnum
    {
        None,
        Server,
        Client,
    }

    public static JsonSerializerOptions options = new JsonSerializerOptions()
    {
        IncludeFields = true,
    };

    public const string Prefix = "CustomPeer";
    public const int MAX_PACKET_SIZE = 4096;//1 << 24;

    [Signal]
    public delegate void PacketGeneratedEventHandler(int peerId, byte[] buffer);

    private WebSocketPeer webSocket = new WebSocketPeer();
    private Queue<Packet> incomingPackets = new Queue<Packet>();
    private int selfId = 0;
    private string selfSessionId = string.Empty;
    private ConnectionStatus connectionStatus = ConnectionStatus.Disconnected;
    private bool refusingNewConnections = false;
    private int targetId = 0;
    private TargetStateEnum targetState = TargetStateEnum.None;
    private string targetRoomId = string.Empty;
    private string targetRoomName = string.Empty;
    private bool targetRoomPublic = false;
    private Dictionary<int, string> peerIdMap = new Dictionary<int, string>();
    private Dictionary<string, User> userMap = new Dictionary<string, User>();

    public string RoomId => targetRoomId;

    public override byte[] _GetPacketScript()
    {
        if (incomingPackets.Count == 0)
            return Array.Empty<byte>();
        return incomingPackets.Dequeue().data;
    }

    public override Error _PutPacketScript(byte[] pBuffer)
    {
        if (connectionStatus != ConnectionStatus.Connected)
            return Error.ConnectionError;
        if (!peerIdMap.ContainsKey(targetId))
            return Error.Ok;
        webSocket.SendText(MetaPacket.Rpc(peerIdMap[targetId], pBuffer).ToString());
        return Error.Ok;
    }

    public override int _GetAvailablePacketCount() => incomingPackets.Count;

    public override int _GetMaxPacketSize() => MAX_PACKET_SIZE;

    public override int _GetPacketChannel() => 0;

    public override int _GetTransferChannel() => 0;

    public override void _SetTransferChannel(int pChannel)
    {
    }

    public override TransferModeEnum _GetTransferMode() => TransferModeEnum.Reliable;

    public override TransferModeEnum _GetPacketMode() => TransferModeEnum.Reliable;

    public override void _SetTransferMode(TransferModeEnum pMode)
    {
    }

    public override void _SetTargetPeer(int pPeer) => targetId = pPeer;

    public override int _GetPacketPeer()
    {
        if (connectionStatus != ConnectionStatus.Connected)
            return 1;
        if (incomingPackets.Count == 0)
            return 1;
        return incomingPackets.Peek().from;
    }

    public override bool _IsServer() => selfId == 1;

    public override int _GetUniqueId() => selfId;

    public override void _SetRefuseNewConnections(bool pEnable) => refusingNewConnections = pEnable;

    public override bool _IsRefusingNewConnections() => refusingNewConnections;

    public override ConnectionStatus _GetConnectionStatus() => connectionStatus;

    public override async void _Close()
    {
        webSocket.Close();
        connectionStatus = ConnectionStatus.Disconnected;
        incomingPackets.Clear();
        peerIdMap.Clear();
        userMap.Clear();
        while (webSocket.GetReadyState() != WebSocketPeer.State.Closed)
        {
            await Task.Delay(100);
            webSocket.Poll();
        }
    }

    public override void _DisconnectPeer(int pPeer, bool pForce)
    {
        if (peerIdMap.TryGetValue(pPeer, out var sessionId))
        {
            RemoveUser(sessionId, pForce);
        }
    }

    public void CreateServer(string url, string name, bool pub)
    {
        webSocket.ConnectToUrl(url);
        selfId = 1;
        targetState = TargetStateEnum.Server;
        targetRoomId = string.Empty;
        targetRoomName = name;
        targetRoomPublic = pub;
        connectionStatus = ConnectionStatus.Connecting;
    }

    public void CreateClient(string url, string roomId)
    {
        webSocket.ConnectToUrl(url);
        selfId = 0;
        targetState = TargetStateEnum.Client;
        targetRoomId = roomId;
        targetRoomName = string.Empty;
        targetRoomPublic = false;
        connectionStatus = ConnectionStatus.Connecting;
    }

    private int NewSessionIdToPeerId(string sessionId)
    {
        int peerId = (int)Mathf.Abs(sessionId.Hash());
        while (peerId <= 1 || peerIdMap.ContainsKey(peerId))
        {
            peerId++;
            if (peerId > int.MaxValue - 16 || peerId <= 0)
            {
                peerId = (int)Mathf.Abs(GD.Randi());
            }
        }
        return peerId;
    }

    private User AddUser(string sessionId, int peerId)
    {
        Log.Print($"{sessionId} {peerId} connected");
        if (peerIdMap.ContainsKey(peerId))
            return null;
        var user = new User()
        {
            peerId = peerId,
            sessionId = sessionId,
        };
        peerIdMap.Add(peerId, sessionId);
        userMap.Add(sessionId, user);
        return user;
    }

    private void RemoveUser(string sessionId, bool force)
    {
        if (userMap.ContainsKey(sessionId))
        {
            int peerId = userMap[sessionId].peerId;
            Log.Print($"{sessionId} {userMap[sessionId].peerId} disconnected");
            if (!force)
                EmitSignal(SignalName.PeerDisconnected, (long)userMap[sessionId].peerId);
            peerIdMap.Remove(userMap[sessionId].peerId);
            userMap.Remove(sessionId);
            if (peerId == 1)
            {
                Close();
            }
        }
    }

    public override void _Poll()
    {
        webSocket.Poll();
        var state = webSocket.GetReadyState();
        if (state == WebSocketPeer.State.Open)
        {
            while (webSocket.GetAvailablePacketCount() > 0)
            {
                var data = webSocket.GetPacket();
                if (data != null)
                {
                    var packet = MetaPacket.FromString(Encoding.UTF8.GetString(data));
                    if (packet.msgId != "rpc")
                        Log.PrintS(packet.msgId);
                    switch (packet.msgId)
                    {
                        case "open":
                            selfSessionId = packet.sessionId;
                            switch (targetState)
                            {
                                case TargetStateEnum.Server:
                                    webSocket.SendText(MetaPacket.Create(targetRoomId, targetRoomName, targetRoomPublic).ToString());
                                    break;
                                case TargetStateEnum.Client:
                                    webSocket.SendText(MetaPacket.Join(targetRoomId).ToString());
                                    selfId = -1;
                                    break;
                            }
                            break;
                        case "joined":
                            Log.Print($"{packet.sessionId} connected");
                            if (packet.sessionId == selfSessionId && selfId == 1 && connectionStatus == ConnectionStatus.Connecting)
                            {
                                connectionStatus = ConnectionStatus.Connected;
                                targetRoomId = packet.roomId;
                                AddUser(selfSessionId, selfId);
                                EmitSignal(SignalName.PeerConnected, selfId);
                            }
                            if (packet.sessionId != selfSessionId && selfId == 1)
                            {
                                var peerId = NewSessionIdToPeerId(packet.sessionId);
                                var isNew = !peerIdMap.ContainsKey(peerId);
                                AddUser(packet.sessionId, peerId);
                                foreach (var user in userMap)
                                {
                                    webSocket.SendText(MetaPacket.MetaSetId(user.Value.sessionId, user.Value.peerId).ToString());
                                }
                                if (isNew)
                                {
                                    EmitSignal(SignalName.PeerConnected, peerId);
                                }
                            }
                            break;
                        case "left":
                            RemoveUser(packet.sessionId, false);
                            break;
                        case "closed":
                            Close();
                            break;
                        case "meta":
                            if (packet.metaType == 1)
                            {
                                if (packet.sessionId == selfSessionId && selfId == -1)
                                {
                                    selfId = packet.peerId;
                                    connectionStatus = ConnectionStatus.Connected;
                                }
                                var res = AddUser(packet.sessionId, packet.peerId);
                                if (res != null)
                                    EmitSignal(SignalName.PeerConnected, packet.peerId);
                            }
                            break;
                        case "rpc":
                            if (packet.sessionId != selfSessionId && userMap.ContainsKey(packet.sessionId))
                            {
                                incomingPackets.Enqueue(new Packet()
                                {
                                    from = userMap[packet.sessionId].peerId,
                                    data = Convert.FromBase64String(packet.data64),
                                });
                            }
                            break;
                    }
                }
            }
        }
        else if (state == WebSocketPeer.State.Closed)
        {
            Close();
        }
    }
}
