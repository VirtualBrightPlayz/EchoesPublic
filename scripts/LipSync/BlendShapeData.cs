using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class BlendShapeData : Resource
{
    [Export]
    public Dictionary<string, float> blendShapeValues = new Dictionary<string, float>();
}