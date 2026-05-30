using Godot;

public partial class TestIDamageSource : Node, IDamageSource
{
    public string AttackerDisplayName => Name;
    public NodePath AbsolutePath => GetPath();
    public DamageType TypeOfDamage => DamageType.Unknown;
}