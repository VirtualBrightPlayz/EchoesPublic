using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class MfccData : Resource
{
    [Export]
    public float[] array = new float[0];
}