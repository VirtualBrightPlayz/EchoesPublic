using System;
using Godot;

[GlobalClass]
public partial class SCP294Drink : Resource
{
    public enum DispenseSound : int
    {
        Cup = 0,
        Fill = 1,
        Strange = 2,
        LoudStrange = 3,
    }

    [Export]
    public string[] phrases = Array.Empty<string>();
    [Export]
    public Color color = Colors.White;
    [Export]
    public bool emission = false;
    [Export]
    public DispenseSound dispenseSound = DispenseSound.Cup;
    [Export]
    public bool damage = false;
    [Export]
    public bool heal = false;
    [Export]
    public float damageAmount = 0f;
    [Export]
    public float healAmount = 0f;
    [Export]
    public GameSound drinkSound;
}