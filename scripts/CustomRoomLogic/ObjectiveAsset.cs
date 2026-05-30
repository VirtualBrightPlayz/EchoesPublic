using System;
using Godot;

[GlobalClass]
public partial class ObjectiveAsset : Resource
{
    [Export]
    public bool isGlobal = true;
    [Export]
    public string key;
    [Export]
    public ObjectiveAsset[] required = Array.Empty<ObjectiveAsset>();
}