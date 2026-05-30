using System.Collections.Generic;
using Godot;

public partial class SnowArea : Area3D
{
    [Export]
    public MeshInstance3D plane;
    [Export]
    public SubViewport viewport;
    [Export]
    public PackedScene spriteScene;
    [Export]
    public ColorRect colorRect;
    [Export]
    public float clearAmount = 0.1f;
    [Export]
    public Node3D particlesRoot;

    public Dictionary<Node3D, Sprite2D> sprites = new Dictionary<Node3D, Sprite2D>();

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(colorRect))
        {
            var amount = (float)delta * clearAmount;
            colorRect.Color = new Color(0f, 0f, 0f, amount);
        }
        foreach (var body in GetOverlappingBodies())
        {
            if (body is IPlayerController plr && plr.Player.HasAuthority)
            {
                var partPos = particlesRoot.GlobalPosition;
                partPos.X = plr.Player.PlayerPosition.X;
                partPos.Z = plr.Player.PlayerPosition.Z;
                particlesRoot.GlobalPosition = partPos;
            }
            if (sprites.ContainsKey(body))
                continue;
            if (body.IsInGroup("snow_body"))
            {
                var sprite = spriteScene.Instantiate<Sprite2D>();
                viewport.AddChild(sprite);
                sprites.Add(body, sprite);
            }
        }
        foreach (var kvp in sprites)
        {
            if (!IsInstanceValid(kvp.Key))
            {
                sprites.Remove(kvp.Key);
                kvp.Value.QueueFree();
                continue;
            }
            UpdateSprite(kvp.Key, kvp.Value, delta);
        }
    }

    public void UpdateSprite(Node3D src, Node2D dst, double delta)
    {
        var pos = plane.GlobalTransform.Inverse().TranslatedLocal(src.GlobalPosition).Origin;
        pos = pos / plane.GetAabb().Size - Vector3.One * 0.5f;
        var spritePos = new Vector2(pos.X, pos.Z) * viewport.Size + viewport.Size * Vector2.One * 1f;
        var dist = 1f - Mathf.Clamp(Mathf.Abs(pos.Y), 0f, 1f);
        dist = 1f;
        dist = 1f - Mathf.Clamp(Mathf.Abs(plane.GlobalPosition.Y - src.GlobalPosition.Y), 0f, 1f);
        dst.Modulate = new Color(1f, 1f, 1f, dist);
        dst.Position = spritePos;
    }
}