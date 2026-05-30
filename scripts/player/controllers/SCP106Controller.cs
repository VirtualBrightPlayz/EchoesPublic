#if false
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class SCP106Controller : FPController, IPlayerSCP
{
    [Export]
    public Node3D[] disableLocal = Array.Empty<Node3D>();
    [Export(PropertyHint.Layers3DRender)]
    public uint localCullFlags = uint.MinValue;

    [Export]
    public RayCast3D playerRayCast;
    [Export]
    public GameSound killSound;
    [Export]
    public GameEffect killedEffect;
    [Export]
    public GameSound teleportedSound;
    [Export]
    public Area3D area;
    [Export]
    public float doorMultiplier = 0.5f;
    [Export]
    public float minDoorDist = 1f;
    public DoorButton foundDoor = null;
    public List<DoorButton> buttons = new List<DoorButton>();
    [Export]
    public float MinPropDist = 1f;
    [Export]
    public float PropMultiplier = 0.5f;
    public PhysicsProp3D foundProp = null;
    public List<PhysicsProp3D> props = new List<PhysicsProp3D>();
    [Export]
    public Timer attackCooldown;
    [Export]
    public Timer teleportCooldown;
    [Export]
    public Timer randomTeleportCooldown;
    [Export]
    public Timer teleportTimer;
    [Export]
    public PackedScene portalScene;
    [Export]
    public float teleportTime = 1.5f;
    [Export]
    public float teleportAnimTime = 1.5f;
    [ExportGroup("Anims")]
    [Export]
    public string stateRiseName;

    public float ExtraSpeed = 1f;

    protected Vector3 lastPosition;
    private bool isTeleporting = false;

    public static StringName door_106 = "door_106";

    public override void _Ready()
    {
        base._Ready();
        if (IsMultiplayerAuthority())
        {
            foreach (var item in disableLocal)
            {
                foreach (var inst in item.FindChildren("*", nameof(VisualInstance3D)))
                {
                    if (inst is VisualInstance3D vis)
                    {
                        vis.Layers = localCullFlags;
                    }
                }
            }
        }
        camera.CullMask = ~localCullFlags;
        if (Multiplayer.IsServer())
        {
            var portals = GetTree().GetNodesInGroup("106_portal");
            foreach (var portal in portals)
                portal.QueueFree();
        }
        isTeleporting = false;
        // 106 rtp cooldown
        randomTeleportCooldown.Start();
        playerRayCast.Enabled = false;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        area.BodyEntered += BodyEnter;
        area.BodyExited += BodyExit;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        area.BodyEntered -= BodyEnter;
        area.BodyExited -= BodyExit;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (model != null && model.anims != null && Player.HasAuthority)
        {
            float targetWalk;
            if (Player.Inputs.MovementDirection.IsZeroApprox())
            {
                targetWalk = 0f;
            }
            else
            {
                targetWalk = Velocity.Length() / Speed;
            }
            // model.anims.walkingDirection = model.anims.walkingDirection.MoveToward(Player.Inputs.MovementDirection, (float)delta * 5f);
            // model.anims.walking = Mathf.MoveToward(model.anims.walking, targetWalk, (float)delta * 2f);
            model.anims.walkingDirection = Player.Inputs.MovementDirection;
            model.anims.walking = targetWalk;
            lastPosition = Position;
        }

        /*
        bool found = false;
        foreach (var body in DoorButton.buttons)
        {
            if (IsInstanceValid(body.door) && body.door.isLocked && !body.door.HasMeta("door_106"))
                continue;
            if (IsInstanceValid(body.door) && body.door.isOpen && !body.door.isMoving)
                continue;
            if (body.HasMeta("door_106") && !body.GetMeta("door_106", false).AsBool())
                continue;
            // if (IsInstanceValid(body.timedDoor))
                // continue;
            // if (body is DoorButton btn)
            if (body.GlobalPosition.DistanceTo(GlobalPosition) < minDoorDist)
            {
                found = true;
                // break;
            }
            if (!buttons.Contains(body))
            {
                buttons.Add(body);
                PhysicsServer3D.BodyAddCollisionException(GetRid(), body.GetRid());
            }
        }
        foundDoor = found;
        bool foundProp = false;
        foreach (var prop in PhysicsProp3D.PhysicsProps)
        {
            if (!prop.Scp106CanPassThrough)
            {
                continue;
            }
            if (prop.GlobalPosition.DistanceTo(GlobalPosition) < MinPropDist)
            {
                foundProp = true;
                // break;
            }
            if (!props.Contains(prop))
            {
                props.Add(prop);
                PhysicsServer3D.BodyAddCollisionException(GetRid(), prop.GetRid());
            }
        }
        this.foundProp = foundProp;
        */
    }
    
    
    public void TryKillAsScp(IPlayerController victim)
    {
        if (victim.Player != null && victim != this && victim.Player.Role.team != Player.Role.team)
        {
            RpcId(1, nameof(TeleportToPD), victim.Player.AbsolutePath);
        }
    }

    private void BodyEnter(Node3D body)
    {
        if (body is DoorButton btn)
        {
            if (IsInstanceValid(btn.door) && btn.door.isLocked && !btn.door.HasMeta(door_106))
                return;
            if (IsInstanceValid(btn.door) && btn.door.isOpen && !btn.door.isMoving)
                return;
            if (body.HasMeta(door_106) && !body.GetMeta(door_106, false).AsBool())
                return;
            PhysicsServer3D.BodyAddCollisionException(GetRid(), btn.GetRid());
            foundDoor = btn;
        }
        else if (body is PhysicsProp3D prop)
        {
            if (!prop.Scp106CanPassThrough)
            {
                return;
            }
            PhysicsServer3D.BodyAddCollisionException(GetRid(), prop.GetRid());
            foundProp = prop;
        }
    }

    private void BodyExit(Node3D body)
    {
        if (body == foundDoor)
        {
            foundDoor = null;
        }
        if (body == foundProp)
        {
            foundProp = null;
        }
    }

    public override void HandleInputs(double delta)
    {
        HandleUse(delta);
        HandleSCP106(delta);
    }

    public void HandleSCP106(double delta)
    {
        playerRayCast.ForceRaycastUpdate();
        if (Player.Inputs.Primary.HasFlag(ButtonInputFlags.JustPressed) && playerRayCast.IsColliding())
        {
            var collider = playerRayCast.GetCollider();
            if (collider != this && collider is Node colliderNode)
            {
                IHealth health = IHealth.GetHealth(collider);
                if (health != null)
                {
                    if (health is IPlayerController victim)
                    {
                        TryKillAsScp(victim);
                    }
                    else if (health is PlayerHitbox hitbox && hitbox.HP is IPlayerController ctrl)
                    {
                        TryKillAsScp(ctrl);
                    }
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void TeleportToPD(NodePath peerId)
    {
        if (Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority() && Multiplayer.IsServer() && attackCooldown.IsStopped())
        {
            var victim = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AbsolutePath == peerId);
            // TODO: ac checks here
            if (victim.Role.team == Player.Role.team)
                return;
            attackCooldown.Start();
            victim.Damage(new DamageInfo(victim.MaxHealth * 0.4f, Player, DamageType.Scp106));
            int idx = Array.IndexOf(Player.Sounds, killSound);
            if (idx != -1)
            {
                Player.Rpc(nameof(NetworkPlayer.CL_PlaySound3D), idx);
            }
            ZoneArea.Zone zone = ZoneArea.GetZone(victim.PlayerPosition);
            var pdSpawn = (Node3D)GetTree().GetFirstNodeInGroup("106_pd");
            victim.Rpc(nameof(NetworkPlayer.Teleport), pdSpawn.GlobalPosition, pdSpawn.GlobalRotation);
            PDLogic.instance.OnEnter(victim, zone);
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
                    int idx = Array.IndexOf(Player.Sounds, teleportedSound);
                    if (idx != -1)
                    {
                        Player.Rpc(nameof(NetworkPlayer.CL_PlaySoundAt3D), idx, node.GlobalPosition);
                    }
                    float time = teleportTime;
                    Rpc(nameof(PortalUsedAsync), time, teleportAnimTime);
                    teleportCooldown.Start();
                    teleportTimer.Start(time / 2f);
                    await ToSignal(teleportTimer, Timer.SignalName.Timeout);
                    Player.Rpc(nameof(NetworkPlayer.Teleport), node.GlobalPosition, new Vector3(Player.PlayerRotation.X, node.GlobalRotation.Y, 0f));
                    break;
                }
            }
        }
    }

    // [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public async void UseRandomTP()
    {
        if (Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority() && Multiplayer.IsServer() && teleportCooldown.IsStopped() && randomTeleportCooldown.IsStopped())
        {
            var playerList = IPlayerList.List(this).PlayerList.Where(x => x.Role.team != TeamID.SCP).ToArray();
            if (playerList.Length == 0)
                return;
            Node3D node = playerList[(int)(GD.Randi() % playerList.Length)].controller;
            PlacePortalImpl(node.GlobalPosition, new Vector3(0f, node.GlobalRotation.Y, 0f));
            int idx = Array.IndexOf(Player.Sounds, teleportedSound);
            if (idx != -1)
            {
                Player.Rpc(nameof(NetworkPlayer.CL_PlaySoundAt3D), idx, node.GlobalPosition);
            }
            float time = teleportTime;
            Rpc(nameof(PortalUsedAsync), time, teleportAnimTime);
            teleportCooldown.Start();
            randomTeleportCooldown.Start();
            teleportTimer.Start(time / 2f);
            await ToSignal(teleportTimer, Timer.SignalName.Timeout);
            Player.Rpc(nameof(NetworkPlayer.Teleport), node.GlobalPosition, new Vector3(Player.PlayerRotation.X, node.GlobalRotation.Y, 0f));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public async void PortalUsedAsync(float time, float animTime)
    {
        if (Multiplayer.GetRemoteSenderId() == 1)
        {
            if (IsMultiplayerAuthority())
            {
                if (Player is NetworkPlayer plr && plr.Hud.roleGUI is SCP106UI ui)
                {
                    ui.Teleported(time);
                    isTeleporting = true;
                }
            }
            else
            {
            }
            await ToSignal(GetTree().CreateTimer(time / 2f - 0.1f), SceneTreeTimer.SignalName.Timeout);
            model.anims.Set($"parameters/{stateRiseName}", (long)AnimationNodeOneShot.OneShotRequest.Fire);
            await ToSignal(GetTree().CreateTimer(time / 2f + 0.1f), SceneTreeTimer.SignalName.Timeout);
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
            isTeleporting = false;
            await ToSignal(GetTree().CreateTimer(animTime), SceneTreeTimer.SignalName.Timeout);
            model.anims.Set($"parameters/{stateRiseName}", (long)AnimationNodeOneShot.OneShotRequest.FadeOut);
        }
    }

    public override float GetSpeedMultiplier()
    {
        if (isTeleporting)
            return 0f;
        if (IsInstanceValid(foundDoor))
        {
            // if (sprintStamina > 0)
            return base.GetSpeedMultiplier() * doorMultiplier * ExtraSpeed;
            // else
                // return base.GetSpeedMultiplier() * doorMultiplier * ExtraSpeed * 0.5f;
        }
        if (IsInstanceValid(foundProp))
        {
            return base.GetSpeedMultiplier() * PropMultiplier * ExtraSpeed;
        }
        return base.GetSpeedMultiplier() * ExtraSpeed;
    }

    public override bool CanSprint()
    {
        if (IsInstanceValid(foundDoor))
        {
            return true;
        }
        return base.CanSprint();
    }

    public override Vector3 GetMovementVelocity(Vector3 vel, double delta)
    {
        return base.GetMovementVelocity(vel, delta);
    }

    public override void OnKilled(DamageInfo info)
    {
        int idx = Array.IndexOf(Player.Effects, killedEffect);
        if (idx != -1)
        {
            Player.Rpc(nameof(NetworkPlayer.CL_PlayEffectAt3D), idx, Player.PlayerPosition, Vector3.Up);
        }
        base.OnKilled(info);
    }
}
#endif