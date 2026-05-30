using Godot;

public partial class KeycardViewmodel : Node3D
{
    public ItemObject Item => GetParent<ItemObject>();

    [Export]
    public Node3D root;
    [Export]
    public Camera3D viewCam;

    [Export]
    public MeshInstance3D mesh;
    [Export]
    public MeshInstance3D textMesh;
    [Export]
    public AnimationTree tree;
    [Export]
    public string statePath = "playback";
    [Export]
    public string usePathV = "use";
    [Export]
    public string usePathH = "use";
    [Export]
    public string startPathV = "EquipV";
    [Export]
    public string startPathH = "EquipH";
    [Export]
    public AudioStreamPlayer3D audio;
    [Export]
    public AudioStream success;
    [Export]
    public AudioStream fail;
    [Export]
    public Node3D keycardPointV;
    [Export]
    public Node3D keycardPointH;

    public bool isHoriz => (Item.Preset as KeycardPreset)?.isHorizAnims ?? false;
    public string usePath => isHoriz ? usePathH : usePathV;
    public string startPath => isHoriz ? startPathH : startPathV;
    public Node3D keycardPoint => isHoriz ? keycardPointH : keycardPointV;

    public void ViewmodelEvent(int id)
    {
        if (!IsVisibleInTree())
            return;
        switch (id)
        {
            case Keycard.ViewmodelEventUse:
            {
                var playback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{statePath}");
                if (IsInstanceValid(playback))
                {
                    playback.Travel(usePath);
                }
                if (IsInstanceValid(audio))
                {
                    audio.Stream = success;
                    audio.Play();
                }
                break;
            }
            case Keycard.ViewmodelEventUseFail:
            {
                var playback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{statePath}");
                if (IsInstanceValid(playback))
                {
                    playback.Travel(usePath);
                }
                if (IsInstanceValid(audio))
                {
                    audio.Stream = fail;
                    audio.Play();
                }
                break;
            }
        }
    }

    public override void _Ready()
    {
        if (IsInstanceValid(Item.Preset) && IsInstanceValid(keycardPoint))
        {
            foreach (var ch in keycardPoint.GetChildren())
            {
                if (IsInstanceValid(ch))
                    ch.QueueFree();
            }
            if (Item.model is Keycard card && IsInstanceValid(card.cardScene))
            {
                var mdl = card.cardScene.Instantiate<Node3D>();
                var meshes = mdl.FindChildren("*", nameof(MeshInstance3D), owned: false);
                foreach (MeshInstance3D mesh in meshes)
                {
                    var mat = mesh.GetActiveMaterial(0).Duplicate() as BaseMaterial3D;
                    mat.UseFovOverride = true;
                    if (IsInstanceValid(viewCam))
                        mat.FovOverride = viewCam.Fov;
                    mat.UseZClipScale = true;
                    mat.ZClipScale = 0.5f;
                    mesh.MaterialOverride = mat;
                    mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                    mesh.IgnoreOcclusionCulling = true;
                }
                keycardPoint.AddChild(mdl);
            }
        }
        var playback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{statePath}");
        if (IsInstanceValid(playback))
        {
            playback.Travel(startPath);
        }
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;
        Camera3D cam = GetViewport().GetCamera3D();
        if (IsInstanceValid(viewCam))
        {
            root.GlobalTransform = (cam.GlobalTransform * viewCam.GlobalTransform.AffineInverse() * root.GlobalTransform);
            // GD.Print(root.GlobalPosition);
            // cam.Transform = viewCam.Transform;
        }
        if (Item.model is Keycard card)
        {
            // mesh.SetSurfaceOverrideMaterial(0, card.viewMaterial);
            if (IsInstanceValid(textMesh) && textMesh.Mesh is TextMesh text)
            {
                text.Text = card.CardHolderName;
            }
        }
    }
}
