using System;
using Godot;

public partial class GFXPass : MeshInstance3D
{
    private GFXBlit blit;
    [Export]
    public SubViewport viewport;
    private double delta;

    public override void _EnterTree()
    {
        RenderingServer.FramePreDraw += PreDraw;
        RenderingServer.FramePostDraw += PostDraw;
        GetViewport().SizeChanged += SizeChange;
        // SizeChange();
        CustomAabb = new Aabb(Vector3.One * -10000f, Vector3.One * 20000f);
    }

    public override void _ExitTree()
    {
        RenderingServer.FramePreDraw -= PreDraw;
        RenderingServer.FramePostDraw -= PostDraw;
        GetViewport().SizeChanged -= SizeChange;
        blit?.Dispose();
    }

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        this.delta = delta;
    }

    public Viewport ActiveViewport()
    {
        // return GetTree().Root;
        return IsInstanceValid(viewport) ? viewport : GetTree().Root;
    }

    private void SizeChange()
    {
        // viewport.Size = ((SubViewport)GetViewport()).Size;
        blit = new GFXBlit(ActiveViewport().GetTexture(), Name);
    }

    private void PreDraw()
    {
    }

    private void PostDraw()
    {
        if (blit == null)
        {
            SizeChange();
            return;
        }
        var mat = GetActiveMaterial(0);
        switch (mat)
        {
            case ShaderMaterial shaderMaterial:
                shaderMaterial.SetShaderParameter("delta", delta);
                shaderMaterial.SetShaderParameter("tex", blit.BlitToTemp());
                break;
        }
    }
}
