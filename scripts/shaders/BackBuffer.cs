using System;
using Godot;

public partial class BackBuffer : Node
{
    [Export]
    public Viewport vp;
    [Export]
    public Camera3D cam;
    [Export]
    public CanvasItem tex;
    [Export]
    public CanvasLayer layer;
    [Export]
    public CanvasItem layerTex;
    [Export(PropertyHint.Range, "0,1")]
    public float amount = 0f;

    public Rid viewport;
    public Rid viewport2;
    public Rid camera;
    public Rid canvas;
    public Rid canvasItem;
    public bool skip;
    public bool use2;
    public Vector2I size;
    public Vector2I texSize;
    public bool wasDraw = false;

    public override void _EnterTree()
    {
        tex.Visible = false;
        layer.Visible = false;
        if (!IsInstanceValid(cam))
        {
            cam = GetParentOrNull<Camera3D>();
        }
        cam.SetMeta("BackBufferBlur", this);
        return;
        if (!IsInstanceValid(vp))
        {
            vp = cam.GetViewport();
        }
        if (!IsInstanceValid(vp))
        {
            vp = GetTree().Root;
        }
        camera = RenderingServer.CameraCreate();
        canvas = RenderingServer.CanvasCreate();
        canvasItem = RenderingServer.CanvasItemCreate();
        viewport = RenderingServer.ViewportCreate();
        viewport2 = RenderingServer.ViewportCreate();
        Resize();
        RenderingServer.ViewportSetCanvasCullMask(vp.GetViewportRid(), 1);
        RenderingServer.ViewportAttachCamera(viewport, camera);
        RenderingServer.ViewportSetDisable2D(viewport, true);
        RenderingServer.ViewportSetParentViewport(viewport, vp.GetViewportRid());
        RenderingServer.ViewportSetEnvironmentMode(viewport, RenderingServer.ViewportEnvironmentMode.Enabled);
        RenderingServer.ViewportSetScenario(viewport, cam.GetWorld3D().Scenario);
        RenderingServer.ViewportSetActive(viewport, true);
        RenderingServer.ViewportAttachCanvas(viewport2, layer.GetCanvas());
        RenderingServer.ViewportSetDisable3D(viewport2, true);
        RenderingServer.ViewportSetActive(viewport2, true);
        vp.SizeChanged += Resize;
        RenderingServer.FramePreDraw += PreDraw;
    }

    public override void _Ready()
    {
        _ExitTree();
        _EnterTree();
    }

    public override void _ExitTree()
    {
        return;
        cam.RemoveMeta("BackBufferBlur");
        vp.SizeChanged -= Resize;
        RenderingServer.FramePreDraw -= PreDraw;
        RenderingServer.FreeRid(canvas);
        canvas = default;
        RenderingServer.FreeRid(canvasItem);
        canvasItem = default;
        RenderingServer.FreeRid(camera);
        camera = default;
        RenderingServer.FreeRid(viewport);
        viewport = default;
        RenderingServer.FreeRid(viewport2);
        viewport2 = default;
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(cam))
        {
            layer.Visible = false;
            return;
        }
        bool draw = cam.Visible && cam.Current && amount > 0;
        layer.Visible = draw;
        layerTex.Modulate = new Color(1f, 1f, 1f, amount);
    }

    private void Resize()
    {
        skip = true;
        size = (Vector2I)vp.GetTexture().GetSize();
        texSize = size;
        RenderingServer.ViewportSetSize(viewport, texSize.X, texSize.Y);
        RenderingServer.ViewportSetSize(viewport2, texSize.X, texSize.Y);
    }

    private void PreDraw()
    {
        bool draw = cam.Visible && cam.Current && amount > 0;
        tex.Visible = draw && wasDraw;
        layer.Visible = draw;
        wasDraw = draw;
        if (!draw)
            return;
        use2 = !use2;
        // skip = false;
        RenderingServer.CameraSetTransform(camera, cam.GlobalTransform);
        RenderingServer.CameraSetUseVerticalAspect(camera, cam.KeepAspect == Camera3D.KeepAspectEnum.Width);
        RenderingServer.CameraSetPerspective(camera, cam.Fov, cam.Near, cam.Far);
        RenderingServer.CameraSetCullMask(camera, cam.CullMask);
        if (IsInstanceValid(cam.Environment))
            RenderingServer.CameraSetEnvironment(camera, cam.Environment.GetRid());
        RenderingServer.ViewportSetUseDebanding(viewport, vp.UseDebanding);
        RenderingServer.ViewportSetUseOcclusionCulling(viewport, vp.UseOcclusionCulling);
        RenderingServer.ViewportSetScaling3DScale(viewport, vp.Scaling3DScale);
        RenderingServer.ViewportSetPositionalShadowAtlasSize(viewport, vp.PositionalShadowAtlasSize, vp.PositionalShadowAtlas16Bits);
        RenderingServer.ViewportSetUpdateMode(viewport, RenderingServer.ViewportUpdateMode.Once);
        RenderingServer.ViewportSetUpdateMode(viewport2, RenderingServer.ViewportUpdateMode.Once);

        RenderingServer.CanvasItemClear(layerTex.GetCanvasItem());
        // if (!skip)
        {
            RenderingServer.CanvasItemAddTextureRect(layerTex.GetCanvasItem(), new Rect2(0, 0, texSize.X, texSize.Y), RenderingServer.ViewportGetTexture(viewport));
            if (!skip)
                RenderingServer.CanvasItemAddTextureRect(layerTex.GetCanvasItem(), new Rect2(0, 0, texSize.X, texSize.Y), RenderingServer.ViewportGetTexture(vp.GetViewportRid()), modulate: new Color(1f, 1f, 1f, amount));
        }
        tex.SelfModulate = new Color(1f, 1f, 1f, amount);
        RenderingServer.CanvasItemClear(tex.GetCanvasItem());
        if (!skip)
            RenderingServer.CanvasItemAddTextureRect(tex.GetCanvasItem(), new Rect2(1, 1, size.X, size.Y), RenderingServer.ViewportGetTexture(viewport2));
        skip = false;
    }
}
