using Godot;
using System;
using System.Linq;

[GlobalClass]
public partial class GameSound : Resource
{
    public static StringName MetaNameRanges = "one_shot_audio_ranges";
    public static StringName MetaName3D = "one_shot_audio_3d";
    public static StringName MetaName = "one_shot_audio";

    [Export]
    public string DisplayName;
    [Export]
    public AudioStream[] streams = new AudioStream[0];
    public AudioStream Stream => streams.FirstOrDefault();
    [Export]
    public AudioStreamSynchronized nearMediumFarStream;
    [Export]
    public float nearDist;
    [Export]
    public float mediumDist;
    [Export]
    public float farDist;
    [Export(PropertyHint.Range, "-80,80")]
    public float volumeDb = 0f;
    [Export(PropertyHint.Range, "0.1,100,or_greater")]
    public float unitSize = 10f;
    [Export(PropertyHint.Range, "0,4096,or_greater")]
    public float maxDistance = 0f;
    [Export(PropertyHint.Range, "-24,6")]
    public float maxDb = 3f;
    [Export(PropertyHint.Range, "0.01,4")]
    public float pitch = 1f;
    [Export]
    public bool forceLoop = false;
    [Export]
    public string bus = string.Empty;

    public GameSound()
    {
    }

    public void PlayOneShotNoPosition(Node node)
    {
        if (forceLoop)
            return;
        AudioStreamPlayer audio;
        {
            if (node.HasMeta(MetaName))
            {
                audio = node.GetMeta(MetaName).As<AudioStreamPlayer>();
            }
            else
            {
                audio = new AudioStreamPlayer();
                node.AddChild(audio);
                node.SetMeta(MetaName, audio);
            }
        }
        audio.Bus = string.IsNullOrEmpty(bus) ? "World" : bus;
        audio.Stream = streams[GD.Randi() % streams.Length];
        audio.VolumeDb = volumeDb;
        audio.PitchScale = pitch;
        audio.Play();
        // await node.ToSignal(audio, AudioStreamPlayer3D.SignalName.Finished);
        // if (IsInstanceValid(audio) && audio != node)
        //     audio.QueueFree();
    }

    public void PlayOneShot3D(Node3D node)
    {
        if (forceLoop)
            return;
        AudioStreamPlayer3D audio;
        if (IsInstanceValid(nearMediumFarStream))
        {
            if (node.HasMeta(MetaNameRanges))
            {
                var plr = node.GetMeta(MetaNameRanges).As<AudioRanges3D>();
                plr.closeMaxRange = nearDist;
                plr.mediumMaxRange = mediumDist;
                plr.farMaxRange = farDist;
                audio = plr;
            }
            else
            {
                audio = new AudioRanges3D()
                {
                    closeMaxRange = nearDist,
                    mediumMaxRange = mediumDist,
                    farMaxRange = farDist,
                };
                node.AddChild(audio);
                // node.SetMeta(MetaNameRanges, audio);
                audio.GlobalPosition = node.GlobalPosition;
            }
            audio.AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.Disabled;
        }
        else
        {
            if (node.HasMeta(MetaName3D))
            {
                audio = node.GetMeta(MetaName3D).As<AudioStreamPlayer3D>();
            }
            else
            {
                audio = new AudioStreamPlayer3D();
                node.AddChild(audio);
                node.SetMeta(MetaName3D, audio);
                audio.GlobalPosition = node.GlobalPosition;
            }
            audio.AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance;
        }
        audio.Bus = string.IsNullOrEmpty(bus) ? "World" : bus;
        if (IsInstanceValid(nearMediumFarStream))
        {
            nearMediumFarStream.ResourceLocalToScene = true;
            audio.Stream = nearMediumFarStream;
            audio.Finished += audio.QueueFree;
        }
        else
        {
            audio.Stream = streams[GD.Randi() % streams.Length];
        }
        audio.UnitSize = unitSize;
        audio.MaxDistance = maxDistance;
        audio.MaxDb = maxDb;
        audio.VolumeDb = volumeDb;
        audio.AttenuationFilterDb = 0f;
        audio.PitchScale = pitch;
        audio.Play();
        // await node.ToSignal(audio, AudioStreamPlayer3D.SignalName.Finished);
        // if (IsInstanceValid(audio) && audio != node)
        //     audio.QueueFree();
    }

    public async void PlayOneShotAt3D(Node3D node, Vector3 pos)
    {
        if (forceLoop)
            return;
        AudioStreamPlayer3D audio;
        if (IsInstanceValid(nearMediumFarStream))
        {
            audio = new AudioRanges3D()
            {
                closeMaxRange = nearDist,
                mediumMaxRange = mediumDist,
                farMaxRange = farDist,
            };
            audio.AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.Disabled;
        }
        else
        {
            audio = new AudioStreamPlayer3D();
            audio.AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance;
        }
        node.AddSibling(audio);
        audio.Bus = string.IsNullOrEmpty(bus) ? "World" : bus;
        audio.GlobalPosition = pos;
        if (IsInstanceValid(nearMediumFarStream))
        {
            nearMediumFarStream.ResourceLocalToScene = true;
            audio.Stream = nearMediumFarStream;
        }
        else
        {
            audio.Stream = streams[GD.Randi() % streams.Length];
        }
        audio.UnitSize = unitSize;
        audio.MaxDistance = maxDistance;
        audio.MaxDb = maxDb;
        audio.VolumeDb = volumeDb;
        audio.AttenuationFilterDb = 0f;
        audio.PitchScale = pitch;
        audio.Play();
        await node.ToSignal(audio, AudioStreamPlayer3D.SignalName.Finished);
        if (IsInstanceValid(audio))
            audio.QueueFree();
    }
}
