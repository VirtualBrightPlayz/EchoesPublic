using System;
using Godot;

[GlobalClass]
public partial class KeycardPreset : ItemPreset
{
    [Export]
    public KeycardAccess[] Access = Array.Empty<KeycardAccess>();
    [Export]
    public Material worldMaterial;
    [Export]
    public Material viewMaterial;
    [Export]
    public PackedScene cardScene;
    [Export]
    public bool isHorizAnims = false;
}
