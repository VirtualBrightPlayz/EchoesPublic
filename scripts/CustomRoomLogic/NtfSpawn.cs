using System;
using Godot;

public partial class NtfSpawn : Node
{
    [Export]
    public AnimationPlayer anim;
    [Export]
    public StringName enterAnim;
    [Export]
    public StringName idleAnim;
    [Export]
    public StringName exitAnim;
    [Export]
    public AudioStreamPlayer3D music;
    [Export]
    public AudioStreamPlayer3D spawnSounds;
    [Export]
    public AudioStreamPlayer3D spawnSoundsIndoors;
    [Export]
    public bool CanPlayMusic = false;

    public bool CanSpawn = false;

    public override void _Ready()
    {
        RoundManager.Instance.Logic.TryRespawnNtf += StartSpawn;
        anim.AnimationChanged += _AnimChanged;
    }

    public override void _ExitTree()
    {
        RoundManager.Instance.Logic.TryRespawnNtf -= StartSpawn;
    }

    public override void _Process(double delta)
    {
        if (IsMultiplayerAuthority() && anim.CurrentAnimation.Equals(idleAnim))
        {
            if (CanSpawn)
            {
                Spawn();
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(music))
        {
            if (IsInstanceValid(NetworkPlayer.LocalInstance))
            {
                ZoneArea.Zone zone = ZoneArea.GetZone(NetworkPlayer.LocalInstance.PlayerPosition);
                if (zone == ZoneArea.Zone.Surface && CanPlayMusic)
                {
                    if (!music.Playing)
                        music.Play();
                }
                else if (music.Playing)
                {
                    music.Stop();
                }
            }
            else
            {
                music.Stop();
            }
        }
    }

    public void StartSpawn()
    {
        if (IsMultiplayerAuthority())
        {
            CanSpawn = true;
            Rpc(MethodName.RpcAnim, 0);
        }
        anim.Play(enterAnim);
        anim.Queue(idleAnim);
    }

    public void Spawn()
    {
        if (IsMultiplayerAuthority())
        {
            CanSpawn = false;
            Rpc(MethodName.RpcAnim, 2);
        }
        anim.Play(exitAnim);
        RoundManager.Instance.Logic.RespawnWave(TeamID.NTF);
    }

    public void SpawnAudio()
    {
        if (IsInstanceValid(NetworkPlayer.LocalInstance))
        {
            ZoneArea.Zone zone = ZoneArea.GetZone(NetworkPlayer.LocalInstance.PlayerPosition);
            if (zone == ZoneArea.Zone.Surface)
            {
                if (IsInstanceValid(spawnSounds))
                    spawnSounds.Play();
            }
            else
            {
                if (IsInstanceValid(spawnSoundsIndoors))
                    spawnSoundsIndoors.Play();
            }
        }
    }

    private void _AnimChanged(StringName oldName, StringName newName)
    {
        if (newName.Equals(idleAnim))
        {
            // Rpc(MethodName.RpcAnim, 1);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    public void RpcAnim(int state)
    {
        switch (state)
        {
            case 0:
                StartSpawn();
                break;
            case 1:
                // anim.Play(idleAnim);
                break;
            case 2:
                Spawn();
                break;
        }
    }
}