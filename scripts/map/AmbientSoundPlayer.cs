using System;
using System.Linq;
using Godot;

[GlobalClass]
public partial class AmbientSoundPlayer : Node
{
    [Export]
    public AudioStream[] Sounds = Array.Empty<AudioStream>();
    [Export]
    public ZoneArea.Zone area;

    public AudioStreamPlayer3D Root => GetParentOrNull<AudioStreamPlayer3D>();

    public async void PlayRandomSound()
    {
        if (IsMultiplayerAuthority())
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            // var list = IPlayerList.List(Root).PlayerList.SelectMany(x => area.GetOverlappingBodies().Where(y => y is IPlayerController && y.IsAncestorOf(x)).Select(y => y as IPlayerController)).ToList();
            var list = IPlayerList.List(Root).PlayerList.Where(x => area == ZoneArea.Zone.Unknown || ZoneArea.GetZone(x.PlayerPosition) == area).ToList();
            if (list.Count == 0)
                return;
            var dir = new Vector3((float)GD.RandRange(-1d, 1d), (float)GD.RandRange(-1d, 1d), (float)GD.RandRange(-1d, 1d)).Normalized() * Vector3.One;
            var pos = list[(int)(GD.Randi() % list.Count)].PlayerPosition + dir;
            Rpc(nameof(RpcPlaySound), GD.Randi(), pos);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcPlaySound(uint index, Vector3 position)
    {
        Root.GlobalPosition = position;
        Root.Stream = Sounds[index % Sounds.Length];
        Root.Play();
    }
}
