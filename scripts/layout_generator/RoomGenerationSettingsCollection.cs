using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class RoomGenerationSettingsCollection : Resource
{
    [Export]
    public Array<RoomGenerationSettings> Collection;
}
