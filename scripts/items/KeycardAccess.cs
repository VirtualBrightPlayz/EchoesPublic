using System;
using Godot;

[GlobalClass]
public partial class KeycardAccess : Resource
{
    [Export]
    public int level;
    [Export]
    public Keycard.CardAccessCategory cardType;
}