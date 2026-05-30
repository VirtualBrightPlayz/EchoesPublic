using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class NetworkPlayer : BasePlayer, IDamageSource, IItemHolder
{
    public static NetworkPlayer LocalInstance { get; private set; }

    public override string AttackerDisplayName => username;
    public DamageType TypeOfDamage => DamageType.Unknown;

    public override bool IsLocalPlayer => IsMultiplayerAuthority();
    public override bool HasAuthority => IsMultiplayerAuthority();
    public override bool IsServer => Multiplayer.IsServer();

    [Export]
    public PlayerConsole console;
    [Export]
    public NodePath serverOwned;
    [Export]
    public NodePath hideLocally;
    [Export]
    public int AuthorityId = 1;
    [Export]
    public int PlayerId = 1;
    [Export]
    public string username;
    [Export]
    public AudioStreamPlayer spawnSoundPlayer;
    [Export]
    public LocalPlayerHUD Hud;
    [Export]
    public LocalPlayerHUDVR HudVR;
    [Export]
    public bool Venting;
    public Action<PlayerRole> OnLocalSpawned { get; set; } = (_) => { };
    [Export]
    public AudioStreamPlayer3D voicePlayback;
    [Export]
    public AudioStreamPlayer globalVoicePlayback;
    [Export]
    public VoiceChat voiceChat;
    [Export]
    public ColorRect glitchEffect;
    [Export]
    public Godot.Collections.Array<string> objectives = new Godot.Collections.Array<string>();
    [Export]
    public Godot.Collections.Array<string> completeObjectives = new Godot.Collections.Array<string>();

    [Export]
    public bool CanNoclip
    {
        get
        {
            if (ActiveController is FPController fp)
                return fp.Noclip;
            return false;
        }
        set
        {
            if (ActiveController is FPController fp)
                fp.Noclip = value;
        }
    }

    [Export]
    public int VoiceChatChannel = 0;

    public float GlitchEffectAmount
    {
        get => ((ShaderMaterial)glitchEffect.Material).GetShaderParameter("amount").AsSingle();
        set => ((ShaderMaterial)glitchEffect.Material).SetShaderParameter("amount", value);
    }

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(AuthorityId);
        base._EnterTree();
        GetNode(serverOwned).SetMultiplayerAuthority((int)MultiplayerPeer.TargetPeerServer);
        (GetNode(hideLocally) as CanvasItem).Visible = HasAuthority;
        if (!HasAuthority)
        {
            Hud.QueueFreeNow();
            HudVR.QueueFreeNow();
        }
        if (HasAuthority)
        {
            LocalInstance = this;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        if (HasAuthority)
        {
            LocalInstance = this;
            IsVR = InputManager.Instance.IsVR;
            RpcId(1, nameof(SV_SetUsername), IInitScript.Instance.Username, IsVR);
        }

        if (Multiplayer.IsServer())
        {
            OnDamaged = (info) => Rpc(nameof(CL_Damaged), DamageInfo.ToNetwork(info));
            SV_Spawn(RoleID.Spectator);
        }
        else
        {
            // CL_Spawn(RoleIndex);
        }

        if (IsLocalPlayer)
        {
            ChangeActionSet("InGame");
        }
    }

    public override void _Process(double delta)
    {
        TryChangeRole();
        base._Process(delta);

        voicePlayback.Position = PlayerPosition;

        if (Role != null && IsInstanceValid(LocalInstance))
        {
            bool shouldGlobalChat = Role.globalTeamChat && Role.team == LocalInstance.Role.team && LocalInstance.Role.listeningChatChannels.Contains(VoiceChatChannel) && VoiceChatChannel != Role.proxChatChannel;
            bool shouldLocalChat = Role.proxChat && LocalInstance.Role.listeningChatChannels.Contains(VoiceChatChannel) && VoiceChatChannel == Role.proxChatChannel;
            globalVoicePlayback.VolumeDb = shouldGlobalChat ? 0f : -80f;
            globalVoicePlayback.SetMeta(Intercom.MuteName, !shouldGlobalChat);
            var voice = voiceChat.GetVoice();
            if (shouldLocalChat && voice == globalVoicePlayback)
            {
                voiceChat.SetVoice(voicePlayback);
            }
            else if (!shouldLocalChat && voice == voicePlayback)
            {
                voiceChat.SetVoice(globalVoicePlayback);
            }
        }

        if (IsLocalPlayer)
        {
            bool voiceChatProx = GetDigitalActionData(LocalPlayerInput.PlayerVoiceChat);
            bool voiceChatAlt = GetDigitalActionData(LocalPlayerInput.PlayerVoiceChatAlt);
            if (voiceChatAlt)
                VoiceChatChannel = Role.altChatChannel;
            else if (voiceChatProx)
                VoiceChatChannel = Role.mainChatChannel;
            voiceChat.Recording = (voiceChatProx || voiceChatAlt) &&
                (!IsInstanceValid(RoundManager.Instance) || RoundManager.Instance.state != RoundManager.RoundState.Loading);
            Hud.MicOn = voiceChat.Recording;
            Hud.MicAmount = voiceChat.Loudness * 80f;
            HudVR.MicOn = voiceChat.Recording;
            HudVR.MicAmount = voiceChat.Loudness * 80f;
        }
    }

    public override void ChangeActionSet(string next)
    {
        if (IsLocalPlayer)
        {
            InputManager.Instance.ChangeActionSet(next);
            return;
        }
        throw new NotMultiplayerAuthorityException();
    }

    public override bool GetDigitalActionData(string action)
    {
        if (IsLocalPlayer)
        {
            return InputManager.Instance.GetDigitalActionData(action);
        }
        throw new NotMultiplayerAuthorityException();
    }

    public override float GetAnalogActionData(string action)
    {
        if (IsLocalPlayer)
        {
            return InputManager.Instance.GetAnalogActionData(action);
        }
        throw new NotMultiplayerAuthorityException();
    }

    public override Vector2 GetStickPadActionData(string action)
    {
        if (IsLocalPlayer)
        {
            return InputManager.Instance.GetStickPadActionData(action);
        }
        throw new NotMultiplayerAuthorityException();
    }

    public void SV_Damage(int sender, float amount, DamageType type)
    {
        if (Multiplayer.IsServer())
        {
            // int sender = Multiplayer.GetRemoteSenderId();
            var list = IPlayerList.List(this).PlayerList;
            var plr = list.FirstOrDefault(x => x.AuthorityId == sender);
            if (plr != null)
            {
                if (plr.Role.team != Role.team)
                {
                    if (ActiveController is IHealth hp)
                        hp.Damage(new DamageInfo(amount, plr, type));
                    else
                        Damage(new DamageInfo(amount, plr, type));
                }
            }
        }
    }

    public void SV_Damage(NodePath sender, float amount, DamageType type)
    {
        if (Multiplayer.IsServer())
        {
            // int sender = Multiplayer.GetRemoteSenderId();
            var list = IPlayerList.List(this).PlayerList;
            var plr = list.FirstOrDefault(x => x.AbsolutePath == sender);
            if (plr != null)
            {
                if (plr.Role.team != Role.team)
                {
                    if (ActiveController is IHealth hp)
                        hp.Damage(new DamageInfo(amount, plr, type));
                    else
                        Damage(new DamageInfo(amount, plr, type));
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void CL_Damaged(Godot.Collections.Dictionary dictInfo)
    {
        if (CallerIsServer && RoleController is IPlayerControllerExt ext)
        {
            DamageInfo info = new DamageInfo(dictInfo);
            ext.OnDamaged(info);
        }
    }

    public override async void ForceTeleport(Vector3 position, Vector3 rotation)
    {
        while (position.DistanceSquaredTo(PlayerPosition) >= 3f)
        {
            if (ActiveController != null)
            {
                ActiveController.Root.GlobalPosition = position;
                // TODO: rotation here
            }
            Rpc(MethodName.Teleport, position, rotation);
            await ToSignal(GetTree().CreateTimer(0.1d), SceneTreeTimer.SignalName.Timeout);
        }
    }

    public override async void ForceTeleportPosition(Vector3 position)
    {
        while (position.DistanceSquaredTo(PlayerPosition) >= 3f)
        {
            if (ActiveController != null)
            {
                ActiveController.Root.GlobalPosition = position;
            }
            Rpc(MethodName.TeleportPosition, position);
            await ToSignal(GetTree().CreateTimer(0.1d), SceneTreeTimer.SignalName.Timeout);
        }
    }

    public override void CL_Spawn(string roleId)
    {
        base.CL_Spawn(roleId);
        if (HasAuthority)
        {
            if (!IsServer)
            {
                foreach (var ability in Abilities)
                {
                    ability.OnSpawn(Role);
                }
            }

            VoiceChatChannel = 0;

            if (!IsInstanceValid(RoundManager.Instance) || (RoundManager.Instance.state == RoundManager.RoundState.InGame || RoundManager.Instance.state == RoundManager.RoundState.End))
            {
                if (IsInstanceValid(Role.SpawnAudio))
                {
                    spawnSoundPlayer.VolumeDb = Role.SpawnAudio.volumeDb;
                    spawnSoundPlayer.Stream = Role.SpawnAudio.streams.FirstOrDefault();
                    spawnSoundPlayer.Play();
                }
            }
            OnLocalSpawned?.Invoke(Data.RoleLookup[RoleId]);

            Hud.RoleUpdated();
            HudVR.RoleUpdated();

            if (IsInstanceValid(AmbientCalculator.Instance))
            {
                AmbientCalculator.Instance.Profiles.Clear();
                AmbientCalculator.Instance.Profiles.Add(Role.Ambient);
            }

            if (IsInstanceValid(RoundManager.Instance) && IsInstanceValid(StatusRpcManager.Instance))
                StatusRpcManager.Instance?.SetActivityRole(Data.RoleLookup[RoleId], true, RoundManager.Instance.ServerSettings.Name, RoundManager.Instance.ServerSettings.IconUrl);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void Teleport(Vector3 position, Vector3 rotation)
    {
        if (CallerIsServer)
        {
            if (ActiveController != null)
            {
                ActiveController.Root.GlobalPosition = position;
                ActiveController.Root.GlobalRotation = rotation with { X = 0f };
                ActiveController.View.Rotation = rotation with { Y = 0f, Z = 0f };
                if (ActiveController.Root is CharacterBody3D body)
                {
                    body.Velocity = Vector3.Zero;
                }
                if (ActiveController.Root is RigidBody3D body2)
                {
                    body2.LinearVelocity = Vector3.Zero;
                    body2.AngularVelocity = Vector3.Zero;
                }
                // ApplyRotation(rotation);
            }
            xformSync.AfterTeleport();
            viewSync.AfterTeleport();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void TeleportPosition(Vector3 position)
    {
        if (CallerIsServer)
        {
            // PlayerPosition = position;
            if (ActiveController != null)
            {
                ActiveController.Root.GlobalPosition = position;
                if (ActiveController.Root is CharacterBody3D body)
                {
                    body.Velocity = Vector3.Zero;
                }
                if (ActiveController.Root is RigidBody3D body2)
                {
                    body2.LinearVelocity = Vector3.Zero;
                    body2.AngularVelocity = Vector3.Zero;
                }
            }
            // xformSync.SendToAll();
            // viewSync.SendToAll();
            xformSync.AfterTeleport();
            viewSync.AfterTeleport();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SV_SetUsername(string newName, bool vr)
    {
        if (CallerHasAuthority && IsServer)
        {
            if (newName.Length > 32)
            {
                newName = newName.Substring(0, 32);
            }
            username = newName;
            IsVR = vr;
            Log.PrintInfo($"Player joined: {username} ({PlayerId})");
            RoundManager.Instance?.OnPlayerJoined(this);
        }
    }

    public override void SV_Spawn(string roleId, Vector3? spawnpoint = null)
    {
        statusEffectManager.DisableEffects();
        Venting = false;
        objectives.Clear();
        completeObjectives.Clear();

        base.SV_Spawn(roleId, spawnpoint);

        if (spawnpoint.HasValue)
        {
            ForceTeleport(spawnpoint.Value, PlayerRotation);
        }
        else if (IsInstanceValid(RoundManager.Instance))
        {
            PlayerSpawnpoint spawn = RoundManager.Instance.Logic.GetSpawn(Role) ?? RoundManager.Instance.Logic.GetSpawn(Role.team);
            ForceTeleport(spawn.GlobalPosition, spawn.GlobalRotation);
        }
        else
        {
            List<PlayerSpawnpoint> spawns = new List<PlayerSpawnpoint>();
            foreach (var sp in GetTree().GetNodesInGroup("spawn"))
            {
                if (sp is PlayerSpawnpoint spawn && spawn.CanSpawn(Role))
                {
                    spawns.Add(spawn);
                }
            }
            if (spawns.Count > 0)
            {
                long idx = GD.Randi() % spawns.Count;
                ForceTeleport(spawns[(int)idx].GlobalPosition, spawns[(int)idx].GlobalRotation);
            }
        }

        TryChangeRole();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void CL_Kick(string reason)
    {
        if (CallerIsServer && HasAuthority)
        {
            MenuManager.Instance.Kicked(reason);
        }
    }

    public void OnDisconnected()
    {
        Log.PrintInfo($"Player left: {username} ({PlayerId})");
        Godmode = false;
        Kill(new DamageInfo());
    }

    public void Kick(string reason)
    {
        int id = GetMultiplayerAuthority();
        GetTree().CreateTimer(0.5f).Timeout += () => NetworkManager.Instance.ForceKick(id);
        RpcId(GetMultiplayerAuthority(), nameof(CL_Kick), reason);
    }
}
