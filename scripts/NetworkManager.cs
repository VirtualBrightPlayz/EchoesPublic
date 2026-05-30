using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

[GlobalClass]
public partial class NetworkManager : SingletonNode3D<NetworkManager>
{
	public enum MultiplayerConnectionState : byte
	{
		NotConnected,
		Connecting,
		Connected,
	}

	public enum DisconnectReason : byte
	{
		Failed,
		User,
		Kicked,
		Version_Difference,
	}

	public struct RelayRoom
	{
		public string id;
		public string name;
		public string size;
	}

	public struct ServerListPost
	{
		public string key { get; set; }
		public string address { get; set; }
		public string name { get; set; }
		public string infoUrl { get; set; }
		public string iconUrl { get; set; }
		public int players { get; set; }
		public int maxPlayers { get; set; }
		public string tags { get; set; }
	}

	public delegate void OnProcessEventHandler(double delta);

	public event OnProcessEventHandler OnProcess;

	public const string BansFilePath = "user://bans.csv";
	public const string VrOnlyTag = "vr-only";
	public const string NoVrOnlyTag = "no-vr-only";

	[Export]
	public string httpHostAddress = "http://127.0.0.1:3000";
	[Export]
	public string wsHostAddress = "ws://127.0.0.1:3000";
	[Export]
	public int tickrate = 30;

	public ENetMultiplayerPeer enetPeer;
	public SteamMultiplayerPeer steamPeer;
	public ProfilerMultiplayerPeer profilerPeer;

	public bool IsServer { get; private set; } = false;
	public bool IsHosting { get; private set; } = false;
	public bool IsClient { get; private set; } = false;
	public MultiplayerConnectionState ConnectionState { get; private set; } = MultiplayerConnectionState.NotConnected;

	public Action SV_Started { get; set; } = () => { };
	public Action SV_Stopped { get; set; } = () => { };
	public Action<int> SV_Connected { get; set; } = (_) => { };
	public Action<int> SV_Disconnected { get; set; } = (_) => { };

	public Action CL_Connected { get; set; } = () => { };
	public Action<DisconnectReason> CL_Disconnected { get; set; } = (_) => { };

	public Dictionary<int, ulong> PingMS = new Dictionary<int, ulong>();
	private Godot.Collections.Dictionary gdPingMs = new Godot.Collections.Dictionary();
	public Dictionary<int, ulong> WaitingForTimeReply = new Dictionary<int, ulong>();

	private ulong lastTickTime;
	public SceneMultiplayer mp;

	public override void _EnterTree()
	{
		base._EnterTree();
	}

	public override void _Ready()
	{
		base._Ready();
		GetTree().MultiplayerPoll = false;
		mp = new SceneMultiplayer();
		// mp.ServerRelay = false;
		enetPeer = new ENetMultiplayerPeer();
		enetPeer.PeerConnected += _Connected;
		enetPeer.PeerDisconnected += _Disconnected;
		mp.ServerDisconnected += Shutdown;
		mp.PeerConnected += _ConnectedFinal;
		steamPeer = new SteamMultiplayerPeer();
		steamPeer._classReference.PeerConnected += _Connected;
		steamPeer._classReference.PeerDisconnected += _Disconnected;
		if (OS.HasFeature("network_profile"))
		{
			profilerPeer = ProfilerMultiplayerPeer.Wrap(enetPeer);
		}
	}

	public override void _ExitTree()
	{
		enetPeer.PeerConnected -= _Connected;
		enetPeer.PeerDisconnected -= _Disconnected;
		steamPeer._classReference.PeerConnected -= _Connected;
		steamPeer._classReference.PeerDisconnected -= _Disconnected;
	}

	public override void _Process(double delta)
	{
		{
			ulong time = Time.GetTicksMsec();
			// if (time - lastTickTime >= 1000d / tickrate)
			{
				Multiplayer.Poll();
				lastTickTime = time;
			}
		}
		if (IsServer)
		{
			int[] peers = Multiplayer.GetPeers();
			for (int i = 0; i < peers.Length; i++)
			{
				if (WaitingForTimeReply.ContainsKey(peers[i]) || peers[i] == Multiplayer.GetUniqueId())
				{
					continue;
				}
				ulong time = Time.GetTicksMsec();
				WaitingForTimeReply[peers[i]] = time;
				// RpcId(peers[i], MethodName.RpcTimestamp, time);
			}
		}
		MultiplayerPeer.ConnectionStatus status = Multiplayer.HasMultiplayerPeer() && Multiplayer.MultiplayerPeer is not OfflineMultiplayerPeer ? Multiplayer.MultiplayerPeer.GetConnectionStatus() : MultiplayerPeer.ConnectionStatus.Disconnected;
		if (ConnectionState == MultiplayerConnectionState.Connecting)
		{
			switch (status)
			{
				case MultiplayerPeer.ConnectionStatus.Disconnected:
					Shutdown(DisconnectReason.Failed);
					break;
				case MultiplayerPeer.ConnectionStatus.Connected:
					ConnectionState = MultiplayerConnectionState.Connected;
					if (IsServer)
					{
						SV_Started?.Invoke();
						if (IsHosting)
							SV_Connected?.Invoke((int)1);
					}
					if (IsClient)
						CL_Connected?.Invoke();
					break;
			}
		}
		if (ConnectionState == MultiplayerConnectionState.Connected && IsServer && status == MultiplayerPeer.ConnectionStatus.Disconnected)
		{
			Shutdown(DisconnectReason.Failed);
			// SV_Stopped?.Invoke();
		}
		OnProcess?.Invoke(delta);
	}

	private void SendPingTo(int peer)
	{
		gdPingMs.Clear();
		foreach (var kvp in PingMS)
		{
			gdPingMs[kvp.Key] = kvp.Value;
		}
		// RpcId(peer, MethodName.RpcPings, peer, gdPingMs);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	private void RpcPings(int peerId, Godot.Collections.Dictionary dict)
	{
		PingMS.Clear();
		foreach (var kvp in dict)
		{
			PingMS[kvp.Key.AsInt32()] = kvp.Value.AsUInt64();
		}
		PingMS[1] = dict[peerId].AsUInt64();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	private void RpcTimestamp(ulong time)
	{
		// RpcId(Multiplayer.GetRemoteSenderId(), MethodName.RpcAckTimestamp, time);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	private async void RpcAckTimestamp(ulong rpctime)
	{
		int sender = Multiplayer.GetRemoteSenderId();
		if (WaitingForTimeReply.TryGetValue(sender, out ulong pastTime))
		{
			ulong time = Time.GetTicksMsec();
			PingMS[sender] = time - pastTime;
			SendPingTo(sender);
			await ToSignal(GetTree().CreateTimer(2d), SceneTreeTimer.SignalName.Timeout);
			WaitingForTimeReply.Remove(sender);
		}
	}

	private void _ConnectedFinal(long id)
	{
		if (IsMultiplayerAuthority())
		{
			SendMods(id);
		}
	}

	private void _Connected(long id)
	{
		if (IsServer)
		{
			if (steamPeer._classReference.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
			{
				ulong steamId = steamPeer.GetSteam64FromPeerId((uint)id);
				// Log.PrintInfoS("Player SteamId64", steamId);
				if (!SteamManager.Instance.LobbyHasSteamId(steamId))
				{
					// KickFromBan((int)id, "Not in lobby");
					// return;
				}
			}
			string peerAddress = GetPeerAddress((int)id);
			if (!string.IsNullOrEmpty(peerAddress))
			{
				if (FileAccess.FileExists(BansFilePath))
				{
					using var file = FileAccess.Open(BansFilePath, FileAccess.ModeFlags.Read);
					string[] keys = file.GetCsvLine();
					int addr = Array.IndexOf(keys, "address");
					int reason = Array.IndexOf(keys, "reason");
					int expires = Array.IndexOf(keys, "expires");
					while (!file.EofReached())
					{
						string[] vals = file.GetCsvLine();
						if (addr != -1)
						{
							if (vals[addr].Equals(peerAddress))
							{
								KickFromBan((int)id, reason == -1 ? string.Empty : vals[reason]);
								return;
							}
						}
					}
				}
			}
			if (!IsHosting || id != 1)
			{
				if (enetPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
				{
					// http://enet.bespin.org/enet_8h.html#adf764cbdea00d65edcd07bb9953ad2b7a9af311f55d4447c4db77307a29c6c1bb
					enetPeer.GetPeer((int)id).SetTimeout(32, 10_000, 30_000);
					enetPeer.GetPeer((int)id).PingInterval(2000);
				}
				SV_Connected?.Invoke((int)id);
			}
		}
		else
		{
			enetPeer.GetPeer((int)id).SetTimeout(32, 10_000, 30_000);
			enetPeer.GetPeer((int)id).PingInterval(2000);
		}
		Log.PrintInfoS(id, "connected");
	}

	private void _Disconnected(long id)
	{
		if (IsServer)
		{
			SV_Disconnected?.Invoke((int)id);
		}
		if (IsClient && Multiplayer.GetUniqueId() == id || id == 1)
		{
			Shutdown(DisconnectReason.Kicked);
		}
		Log.PrintInfoS(id, "disconnected");
	}

	public void Shutdown()
	{
		Shutdown(DisconnectReason.User);
	}

	public void Shutdown(DisconnectReason reason)
	{
		if (ConnectionState == MultiplayerConnectionState.NotConnected && reason == DisconnectReason.User)
			return;
		Log.PrintInfoS("Network Shutdown");
		if (IsServer)
			SV_Stopped?.Invoke();
		if (IsClient)
			CL_Disconnected?.Invoke(reason);
		SteamManager.Instance.DestroyLobby();
		Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
		enetPeer.Close();
		steamPeer._classReference.Close();
		ConnectionState = MultiplayerConnectionState.NotConnected;
		IsServer = false;
		IsHosting = false;
		IsClient = false;
		PingMS.Clear();
		WaitingForTimeReply.Clear();
	}

	public void HostEnet(int port = 27015, int maxClients = 20)
	{
		if (ServerEnet(port, maxClients))
		{
			SV_Connected?.Invoke(1);
			Log.PrintInfoS("Now hosting on port", port);
		}
	}

	public bool ServerEnet(int port = 27015, int maxClients = 20)
	{
		Shutdown();
		IsServer = true;
		IsHosting = false;
		IsClient = false;
		enetPeer.SetBindIP("*");
		Error err = enetPeer.CreateServer(port, maxClients, 0);
		if (err != Error.Ok)
		{
			Shutdown(DisconnectReason.Failed);
			return false;
		}
		if (OS.HasFeature("network_profile"))
		{
			Multiplayer.MultiplayerPeer = profilerPeer;
		}
		else
		{
			Multiplayer.MultiplayerPeer = enetPeer;
		}
		ConnectionState = MultiplayerConnectionState.Connected;
		SV_Started?.Invoke();
		Log.PrintInfoS("Now listening on port", port);
		return true;
	}

	public void ClientEnet(string addr = "127.0.0.1", string servername = null)
	{
		Shutdown();
		IsServer = false;
		IsHosting = false;
		IsClient = true;

		string[] args = addr.Split(':');
		string ip = (args.Length > 0) ? string.Join(':', args[0]) : addr;
		int port = (int.TryParse(args[^1], out int res)) ? res : 27015;

		Log.PrintInfo("Now joining ", ip, ":", port);

		Error err = enetPeer.CreateClient(ip, port, 0);
		if (err != Error.Ok)
		{
			Shutdown(DisconnectReason.Failed);
			return;
		}
		if (OS.HasFeature("network_profile"))
		{
			Multiplayer.MultiplayerPeer = profilerPeer;
		}
		else
		{
			Multiplayer.MultiplayerPeer = enetPeer;
		}
		ConnectionState = MultiplayerConnectionState.Connecting;
	}


	public void HostSteam(string name, GodotSteam.Steam.LobbyType type, int maxClients = 20)
	{
		Shutdown();
		SteamManager.Instance.CreateLobby(type, maxClients, name);
		return;
		IsServer = true;
		IsHosting = true;
		IsClient = false;
		Log.PrintInfo("Now creating match");
		Error err = steamPeer.CreateHost(27015);
		if (err == Error.Ok)
		{
			Multiplayer.MultiplayerPeer = steamPeer._classReference;
			ConnectionState = MultiplayerConnectionState.Connected;
			SV_Started?.Invoke();
			SV_Connected?.Invoke(1);
			SteamManager.Instance.CreateLobby(type, maxClients, name);
		}
		else
		{
			Shutdown(DisconnectReason.Failed);
		}
	}

	public void OnSteamLobbyCreated(ulong id)
	{
		IsServer = true;
		IsHosting = true;
		IsClient = false;
		Log.PrintInfo("Now creating match");
		Error err = steamPeer.HostWithLobby(id);
		if (err == Error.Ok)
		{
			Multiplayer.MultiplayerPeer = steamPeer._classReference;
			ConnectionState = MultiplayerConnectionState.Connected;
			SV_Started?.Invoke();
			SV_Connected?.Invoke(1);
		}
		else
		{
			Log.PrintErr($"Failed to create match ({err})");
			Shutdown(DisconnectReason.Failed);
		}
	}

	public void ClientSteam(ulong id)
	{
		Shutdown();
		SteamManager.Instance.JoinLobby(id);
	}

	public void OnSteamLobbyJoined(ulong id)
	{
		IsServer = false;
		IsHosting = false;
		IsClient = true;
		Log.PrintInfo($"Now joining match with id {id}");
		Error err = steamPeer.ConnectToLobby(id);
		if (err == Error.Ok)
		{
			Multiplayer.MultiplayerPeer = steamPeer._classReference;
			ConnectionState = MultiplayerConnectionState.Connecting;
		}
		else
		{
			Log.PrintErr($"Failed to connect to {id} ({err})");
			Shutdown(DisconnectReason.Failed);
		}
	}

	public async void PublicServerList(Action<ServerListPost[]> onServerListings, Action onDone)
	{
		try
		{
			var response = Encoding.UTF8.GetString(await HttpUtils.HttpGetAsync(this, $"{httpHostAddress}/api/v2/serverlist"));
			var node = JsonNode.Parse(response);
			var arr = node["list"].AsArray();
			var list = new ServerListPost[arr.Count];
			for (int i = 0; i < arr.Count; i++)
			{
				var m = arr[i].AsObject();
				list[i] = new ServerListPost()
				{
					address = m["address"]?.ToString(),
					name = m["name"]?.ToString(),
					infoUrl = m["infoUrl"]?.ToString(),
					iconUrl = m["iconUrl"]?.ToString(),
					players = m["players"]?.ToString()?.ToInt() ?? -1,
					maxPlayers = m["maxPlayers"]?.ToString()?.ToInt() ?? -1,
					tags = string.Join(',', m["tags"]?.AsArray()?.Select(x => x.ToString()) ?? Array.Empty<string>()),
				};
			}
			onServerListings?.Invoke(list);
		}
		catch (Exception e)
		{
			Log.PrintErr(e);
		}
		finally
		{
			onDone?.Invoke();
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void CL_Ban(string reason)
	{
		MenuManager.Instance.Banned(reason);
	}

	public void SendMods(long id)
	{
		if (IsInstanceValid(ModLoader.Instance))
		{
			List<string> mids = new List<string>();
			List<long> wids = new List<long>();
			for (int i = 0; i < ModLoader.Instance.Mods.Count; i++)
			{
				if (ModLoader.Instance.Mods[i].Loaded)
				{
					mids.Add(ModLoader.Instance.Mods[i].Info.ModId);
					wids.Add(ModLoader.Instance.Mods[i].WorkshopFileId);
					Log.Print($"Mod {mids[^1]}, {wids[^1]}");
				}
			}
			RpcId(id, MethodName.CL_SetMods, new Godot.Collections.Array<string>(mids), new Godot.Collections.Array<long>(wids));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	public void CL_SetMods(Godot.Collections.Array<string> modIds, Godot.Collections.Array<long> modSteamIds)
	{
		if (!IsInstanceValid(ModLoader.Instance))
			return;
		ModLoader.Instance.UnloadAllMods();
		List<string> missing = ModLoader.Instance.LoadModList(modIds.ToArray());
		if (missing.Count != 0)
		{
			Log.PrintErr($"Missing mods: {string.Join(',', missing)}");
			MenuManager.Instance.MissingMods(string.Join(", ", missing));
			for (int i = 0; i < missing.Count; i++)
			{
				int index = modIds.IndexOf(missing[i]);
				if (index != -1 && modSteamIds[index] != 0)
				{
					SteamManager.Instance.InstallWorkshopItem(modSteamIds[index]);
				}
			}
			return;
		}
	}

	public async void KickFromBan(int id, string reason)
	{
		if (id == 0 || id == 1)
			return;
		await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
		RpcId(id, nameof(CL_Ban), reason);
		await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
		ForceKick(id);
	}

	public void ForceKick(int id)
	{
		Multiplayer.MultiplayerPeer.DisconnectPeer(id);
	}

	public string GetPeerAddress(int id)
	{
		if (enetPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
		{
			return enetPeer.GetPeer(id).GetRemoteAddress();
		}
		return string.Empty;
	}

	public string GetPeerAddressAndPort(int id)
	{
		if (enetPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
		{
			return enetPeer.GetPeer(id).GetRemoteAddress() + ':' + enetPeer.GetPeer(id).GetRemotePort();
		}
		return string.Empty;
	}

	public void BanAddress(string address, string banReason)
	{
		if (enetPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
		{
			if (!FileAccess.FileExists(BansFilePath))
			{
				using var file2 = FileAccess.Open(BansFilePath, FileAccess.ModeFlags.Write);
				file2.StoreCsvLine(new[] { "address", "reason", "expires" });
			}
			using var file = FileAccess.Open(BansFilePath, FileAccess.ModeFlags.ReadWrite);
			string[] keys = file.GetCsvLine();
			int addr = Array.IndexOf(keys, "address");
			int reason = Array.IndexOf(keys, "reason");
			int expires = Array.IndexOf(keys, "expires");
			file.SeekEnd();
			string[] vals = new string[keys.Length];
			Array.Fill(vals, string.Empty);
			if (addr != -1)
				vals[addr] = address;
			if (reason != -1)
				vals[reason] = banReason;
			file.StoreCsvLine(vals);
		}
	}

	public void BanPeer(int id, string banReason)
	{
		if (enetPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected && id != 1)
		{
			BanAddress(GetPeerAddress(id), banReason);
		}
	}

	private static bool CheckServerVersion(string serverVersion)
	{
		if (serverVersion == null)
			return false;

		return ProjectSettings
			.GetSetting("application/config/version")
			.ToString()
			.Equals(serverVersion);
	}
}
