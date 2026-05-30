using Godot;
using System;

[GlobalClass]
public partial class GameEffect : Resource
{
    [Export]
    public string DisplayName;
    [Export]
    public PackedScene scene;
    [Export]
    public float Lifetime = 1f;

    public async void PlayOneShot3D(Node3D node, bool child)
    {
        Node3D n = scene.Instantiate<Node3D>();
        if (child)
            node.AddChild(n);
        else
            node.AddSibling(n);
        n.GlobalPosition = node.GlobalPosition;
        n.GlobalRotation = node.GlobalRotation;
        if (Lifetime <= 0)
            return;
        await node.ToSignal(node.GetTree().CreateTimer(Lifetime), SceneTreeTimer.SignalName.Timeout);
        if (IsInstanceValid(n))
            n.QueueFree();
    }

    public async void PlayOneShotAt3D(Node node, Vector3 position, Vector3 rotation, bool child)
    {
        Node3D n = scene.Instantiate<Node3D>();
        if (child)
            node.AddChild(n);
        else
            node.AddSibling(n);
        n.GlobalPosition = position;
        n.GlobalRotation = rotation;
        if (Lifetime <= 0)
            return;
        await node.ToSignal(node.GetTree().CreateTimer(Lifetime), SceneTreeTimer.SignalName.Timeout);
        if (IsInstanceValid(n))
            n.QueueFree();
    }

    public Node3D SpawnAt3D(Node node, Vector3 position, Vector3 rotation, bool child)
    {
        Node3D n = scene.Instantiate<Node3D>();
        if (child)
            node.AddChild(n);
        else
            node.AddSibling(n);
        n.GlobalPosition = position;
        n.GlobalRotation = rotation;
        return n;
    }
}
