using System.Collections.Generic;
using Godot;

public partial class PlayerSpray : Node3D
{
    [Export]
    public Decal decal;
    [Export]
    public string url;

    public override void _Ready()
    {
        if (SprayManager.Instance.SprayTextureCache.TryGetValue(url, out ImageTexture tex))
        {
            decal.TextureAlbedo = tex;
        }
    }
}