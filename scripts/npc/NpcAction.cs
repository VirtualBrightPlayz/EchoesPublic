using Godot;

[GlobalClass]
public partial class NpcAction : Resource
{
    [Export] public string ActionName;
    [Export] public Godot.Collections.Dictionary<string, bool> PreConditions = new();
    [Export] public Godot.Collections.Dictionary<string, bool> PostEffects = new();
    [Export] public float Cost = 1f;
}