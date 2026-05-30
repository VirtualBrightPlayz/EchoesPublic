using System;
using Godot;

[GlobalClass]
public partial class RoomInfoData : Resource
{
    [Export] public Godot.Collections.Array<RoomPropertyData> templateData = new Godot.Collections.Array<RoomPropertyData>();
}