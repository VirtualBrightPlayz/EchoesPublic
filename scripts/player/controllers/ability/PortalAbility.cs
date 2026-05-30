using System;
using Godot;

[GlobalClass]
public partial class PortalAbility : BaseAbility
{
    [Export] public SCP106UI ui;
    [Export] public CanvasLayer layer;
    [Export] public Timer teleportCooldown;
    [Export] public Timer teleportTimer;
    [Export] public PackedScene portalScene;
    [Export] public float teleportTime = 1.5f;
    [Export] public float teleportAnimTime = 1.5f;
    [Export] public GameSound teleportedSound;
    [ExportGroup("Anims")]
    [Export] public string stateRiseName;

    public bool isTeleporting = false;
    public bool IsUIOpen = false;
    public ButtonInputFlags buttonInventory = ButtonInputFlags.None;

    public override void SetupFromDefinition()
    {
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!Player.IsLocalPlayer)
        {
            layer.Visible = false;
            return;
        }
        layer.Visible = IsUIOpen;
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerInventory, ref buttonInventory);
        if (buttonInventory.HasFlag(ButtonInputFlags.JustPressed))
        {
            IsUIOpen = !IsUIOpen;
            Player.ChangeActionSet(IsUIOpen ? "Inventory" : "InGame");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void PlacePortal()
    {
        if (Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority() && Multiplayer.IsServer() && teleportCooldown.IsStopped())
        {
            PlacePortalImpl(Player.PlayerPosition, new Vector3(0f, Player.PlayerRotation.Y, 0f));
            teleportCooldown.Start();
        }
    }

    public void PlacePortalImpl(Vector3 pos, Vector3 rot)
    {
        var portals = GetTree().GetNodesInGroup("106_portal");
        foreach (var portal in portals)
            portal.QueueFree();
        var node = portalScene.Instantiate<Node3D>();
        ItemManager.Instance.SpawnNode.AddChild(node, true);
        node.GlobalPosition = pos;
        node.GlobalRotation = rot;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public async void UsePortal()
    {
        if (Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority() && Multiplayer.IsServer() && teleportCooldown.IsStopped())
        {
            var portals = GetTree().GetNodesInGroup("106_portal");
            foreach (var portal in portals)
            {
                if (portal is Node3D node)
                {
                    int idx = Player.Data.Sounds.IndexOf(teleportedSound);
                    if (idx != -1)
                    {
                        Player.Rpc(BasePlayer.MethodName.CL_PlaySoundAt3D, idx, node.GlobalPosition);
                    }
                    float time = teleportTime;
                    Rpc(MethodName.PortalUsedAsync, time, teleportAnimTime);
                    teleportCooldown.Start();
                    teleportTimer.Start(time / 2f);
                    await ToSignal(teleportTimer, Timer.SignalName.Timeout);
                    Player.ForceTeleportPosition(node.GlobalPosition);
                    break;
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public async void PortalUsedAsync(float time, float animTime)
    {
        if (Multiplayer.GetRemoteSenderId() == MultiplayerPeer.TargetPeerServer)
        {
            if (IsMultiplayerAuthority() && Player is NetworkPlayer plr)
            {
                ui.Teleported(time);
                isTeleporting = true;
                if (Player.ActiveController is FPController fp)
                {
                    fp.ExtraSpeed = 0f;
                }
            }
            bool hasModel = IsInstanceValid(Player.model) && IsInstanceValid(Player.model.anims);
            await ToSignal(GetTree().CreateTimer(time / 2f - 0.1f), SceneTreeTimer.SignalName.Timeout);
            if (hasModel)
            {
                Player.model.anims.Set($"parameters/{stateRiseName}", (long)AnimationNodeOneShot.OneShotRequest.Fire);
            }
            await ToSignal(GetTree().CreateTimer(time / 2f + 0.1f), SceneTreeTimer.SignalName.Timeout);
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
            isTeleporting = false;
            if (Player.ActiveController is FPController fp2)
            {
                fp2.ExtraSpeed = 1f;
            }
            await ToSignal(GetTree().CreateTimer(animTime), SceneTreeTimer.SignalName.Timeout);
            if (hasModel)
            {
                Player.model.anims.Set($"parameters/{stateRiseName}", (long)AnimationNodeOneShot.OneShotRequest.Abort);
            }
        }
    }
}
