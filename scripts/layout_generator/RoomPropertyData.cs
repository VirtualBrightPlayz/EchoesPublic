using System;
using Godot;

[GlobalClass]
public partial class RoomPropertyData : Resource
{
    public enum PropType : int
    {
        String = 0,
        Int = 1,
        Float = 2,
        Bool = 3,
    }

    [Export] public string key;
    [Export] public PropType type;
    [Export] public string value;
}