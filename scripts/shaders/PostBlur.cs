using Godot;
using System;
using System.Collections.Generic;

public partial class PostBlur : Node
{
    private List<GFXBlit> blits = new List<GFXBlit>();
    private int frame = 0;
    private Rid rdArray;
    private RenderingDevice dev;

    [Export]
    public bool Visible = true;
    [Export]
    public int pastFrames = 1;
    [Export]
    public int frameBlitInterval = 1;

    public override void _EnterTree()
    {
        dev = RenderingServer.GetRenderingDevice();
        if (dev == null)
            QueueFree();
        RenderingServer.FramePreDraw += PreDraw;
        RenderingServer.FramePostDraw += PostDraw;
        ActiveViewport().SizeChanged += SizeChange;
    }

    public override void _ExitTree()
    {
        if (rdArray.IsValid)
            dev.FreeRid(rdArray);
        RenderingServer.FramePreDraw -= PreDraw;
        RenderingServer.FramePostDraw -= PostDraw;
        ActiveViewport().SizeChanged -= SizeChange;
        foreach (var blit in blits)
            blit?.Dispose();
        blits.Clear();
        frame = 0;
    }

    public Viewport ActiveViewport()
    {
        return GetTree().Root;
    }

    public ShaderMaterial ActiveMaterial()
    {
        switch (GetParent())
        {
            case MeshInstance3D mesh:
                return mesh.GetActiveMaterial(0) as ShaderMaterial;
            case CanvasItem canvasItem:
                return canvasItem.Material as ShaderMaterial;
        }
        return null;
    }

    public void SetVisible(bool value)
    {
        switch (GetParent())
        {
            case MeshInstance3D mesh:
                mesh.Visible = value;
                break;
            case CanvasItem canvasItem:
                canvasItem.Visible = value;
                break;
        }
    }

    private void SizeChange()
    {
        var size = ActiveViewport().GetTexture().GetSize();
        if (rdArray.IsValid)
            dev.FreeRid(rdArray);
        rdArray = dev.TextureCreate(new RDTextureFormat()
        {
            Width = (uint)size.X,
            Height = (uint)size.Y,
            Depth = 1,
            ArrayLayers = (uint)pastFrames,
            TextureType = RenderingDevice.TextureType.Type2DArray,
            Mipmaps = 1,
            Samples = RenderingDevice.TextureSamples.Samples1,
            Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
            UsageBits = RenderingDevice.TextureUsageBits.ColorAttachmentBit | RenderingDevice.TextureUsageBits.SamplingBit,
        }, new RDTextureView()
        {
            FormatOverride = RenderingDevice.DataFormat.Max,
        });
        if (!rdArray.IsValid)
            Log.PrintErr("Texture 2D Array invalid!");

        foreach (var blit in blits)
            blit?.Dispose();
        blits.Clear();
        frame = 0;
        blits.Add(new GFXBlit(ActiveViewport().GetTexture(), rdArray, 0, $"{Name}_0"));
        for (int i = 1; i < pastFrames; i++)
        {
            // if (i == pastFrames - 1)
            //     blits.Add(new GFXBlit(blits[i-1].RdTextureRid, default, -1, $"{Name}_{i}"));
            // else
                blits.Add(new GFXBlit(blits[i-1].RdTextureRid, rdArray, i, $"{Name}_{i}"));
        }
    }

    private void PreDraw()
    {
    }

    private void PostDraw()
    {
        var mat = ActiveMaterial();
        SetVisible(false);
        if (blits == null || blits.Count == 0)
        {
            SizeChange();
            return;
        }
        if (!Visible)
            return;
        frame++;
        SetVisible(true);
        if (frame % frameBlitInterval == 0)
        {
            for (int i = pastFrames - 1; i >= 0; i--)
            {
                blits[i].BlitToTemp();
            }
        }
        var dev = RenderingServer.GetRenderingDevice();
        var array = new Texture2DArrayRD()
        {
            TextureRdRid = rdArray,
        };
        mat.SetShaderParameter("count", pastFrames);
        mat.SetShaderParameter("main_tex", array);
    }
}
