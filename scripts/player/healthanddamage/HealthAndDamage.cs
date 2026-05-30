using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using Godot.Collections;

public interface IHealth
{
    public static StringName MetaName = "ihealth";

    public static IHealth GetHealth(GodotObject node)
    {
        if (node is IHealth hp)
            return hp;
        return GodotObject.IsInstanceValid(node) && node.HasMeta(MetaName) ? node.GetMeta(MetaName).AsGodotObject() as IHealth : null;
    }

    public static IHealth GetHealthNotHitbox(GodotObject node)
    {
        if (node is PlayerHitbox hb)
            return hb.HP;
        if (node is IHealth hp)
            return hp;
        return GodotObject.IsInstanceValid(node) && node.HasMeta(MetaName) ? node.GetMeta(MetaName).AsGodotObject() as IHealth : null;
    }

    float Health { get; set; }
    float MaxHealth { get; set; }
    void Spawn(HealInfo info);
    void Damage(DamageInfo info);
    void Heal(HealInfo info);
    void Kill(DamageInfo info);
}

public interface IHealSource
{
    string AttackerDisplayName { get; }
    NodePath AbsolutePath { get; }
}

public interface IDamageSource
{
    string AttackerDisplayName { get; }
    NodePath AbsolutePath { get; }
    // DamageType TypeOfDamage { get; }
}

public enum DamageType : byte
{
    Unknown = 0,
    Crushed = 1,
    Scp173 = 2,
    Scp106 = 3,
    GunLight = 4,
    GunHeavy = 5,
    Gas = 6,
    Nuke = 7,
    Scp457 = 8,
    Fire = 9,
    Scp207 = 10,
    Scp049_2 = 11,
    Scp008 = 12,
    Fall = 13,
    ElectricShock = 14,
    Scp049 = 15,
    Generic = 16,
    Water = 17,
}

public static class DamageUtils
{
    public static string TranslateType(DamageType type)
    {
        return MainMenuUI.TranslateText($"DAMAGE_CAUSE_{type.ToString().ToUpper()}");
    }

    public static string TranslateDeathMsg(DamageType type)
    {
        return MainMenuUI.TranslateText($"DEATH_MSG_{type.ToString().ToUpper()}");
    }
}

public interface IHealthModifier
{
    public float Amount { get; set; }
}

public partial class HealInfo : RefCounted, IHealthModifier
{
    [Export]
    public float Amount { get; set; }
    [Export]
    public NodePath AbsolutePath
    {
        get => IsInstanceValid((GodotObject)Source) ? Source?.AbsolutePath : null;
        set => Source = ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull(value) as IHealSource;
    }
    public IHealSource Source { get; set; }

    public HealInfo()
    {
        Amount = 0f;
        Source = null;
    }

    public HealInfo(float amount)
    {
        Amount = amount;
        Source = null;
    }

    public HealInfo(float amount, IHealSource source)
    {
        Amount = amount;
        Source = source;
    }

    public HealInfo(float amount, NodePath path)
    {
        if (!path.IsAbsolute())
            throw new InvalidOperationException($"The specified path \"{path}\" is relative, add the current node as a parameter to the constructor.");
        Amount = amount;
        AbsolutePath = path;
    }

    public HealInfo(float amount, NodePath path, Node current)
    {
        Amount = amount;
        AbsolutePath = current.GetNodeOrNull(path).GetPath();
    }

    public HealInfo(Dictionary dict)
    {
        foreach (var prop in GetPropertyList())
        {
            var name = prop["name"].AsString();
            var flags = prop["usage"].As<PropertyUsageFlags>();
            if (flags.HasFlag(PropertyUsageFlags.ScriptVariable))
                Set(name, dict[name]);
        }
    }

    public static Dictionary ToNetwork(HealInfo info)
    {
        var dict = new Dictionary();
        if (IsInstanceValid(info))
        {
            foreach (var prop in info.GetPropertyList())
            {
                var name = prop["name"].AsString();
                var flags = prop["usage"].As<PropertyUsageFlags>();
                if (flags.HasFlag(PropertyUsageFlags.ScriptVariable))
                    dict[name] = info.Get(name);
                // GD.PrintS(name, flags, flags.HasFlag(PropertyUsageFlags.ScriptVariable));
            }
        }
        return dict;
    }

    public override string ToString()
    {
        return $"HealInfo({Amount}, {Source?.AbsolutePath ?? "null"})";
    }
}

public abstract class DamageInfoModifier
{
    public DamageInfoModifier()
    {
        
    }
    
    public DamageInfo OwnerInfo { get; set; }
    
    public abstract bool Active { get; set; }

    public abstract void Apply(Node node);

    public abstract void ApplyPostMortem(Node node);
}

public partial class DamageInfo : RefCounted, IHealthModifier
{
    [Export]
    public float Amount { get; set; }
    
    [Export]
    public NodePath SourceAbsolutePath
    {
        get => IsInstanceValid((GodotObject)Source) ? Source?.AbsolutePath : null;
        set => Source = ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull(value) as IDamageSource;
    }
    
    [Export]
    public NodePath TargetAbsolutePath
    {
        get => IsInstanceValid((GodotObject)Target) ? ((Node)Target)?.GetPath() : null;
        set => Target = ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull(value) as IHealth;
    }
    
    [Export]
    public int TypeId { get; set; }

    [Export]
    public int TeamId { get; set; } = (int)TeamID.Dead;

    public IHealth Hitbox { get; set; }

    public IHealth Target { get; set; }
    
    private List<DamageInfoModifier> _modifers = new List<DamageInfoModifier>();

    public void AddModifier(DamageInfoModifier modifier)
    {
        modifier.OwnerInfo = this;
        if (!_modifers.Contains(modifier))
        {
            _modifers.Add(modifier);
        }
    }

    public void RemoveModifier(DamageInfoModifier modifier)
    {
        if (_modifers.Contains(modifier))
        {
            _modifers.Remove(modifier);
        }
    }
    
    public ImmutableList<DamageInfoModifier> Modifiers => _modifers.ToImmutableList();
    
    public IDamageSource Source { get; set; }
    public DamageType TypeOfDamage
    {
        get => (DamageType)TypeId;
        set => TypeId = (int)value;
    }
    public TeamID Team
    {
        get => (TeamID)TeamId;
        set => TeamId = (int)value;
    }

    public DamageInfo()
    {
        Amount = 0f;
        Source = null;
        TypeOfDamage = DamageType.Unknown;
    }

    public DamageInfo(float amount)
    {
        Amount = amount;
        Source = null;
        TypeOfDamage = DamageType.Unknown;
    }
    
    public DamageInfo(float amount, DamageType typeOfDamage)
    {
        Amount = amount;
        Source = null;
        TypeOfDamage = typeOfDamage;
    }

    public DamageInfo(float amount, IDamageSource source, DamageType type)
    {
        Amount = amount;
        Source = source;
        TypeOfDamage = type;
    }

    public DamageInfo(float amount, IDamageSource source, DamageType type, TeamID team)
    {
        Amount = amount;
        Source = source;
        TypeOfDamage = type;
        Team = team;
    }

    public DamageInfo(float amount, NodePath path, DamageType type, TeamID team)
    {
        if (path != null && !path.IsAbsolute())
            throw new InvalidOperationException($"The specified path \"{path}\" is relative, add the current node as a parameter to the constructor.");
        Amount = amount;
        SourceAbsolutePath = path;
        TypeOfDamage = type;
        Team = team;
    }

    public DamageInfo(float amount, NodePath path, Node current, DamageType type)
    {
        Amount = amount;
        SourceAbsolutePath = current.GetNodeOrNull(path).GetPath();
        TypeOfDamage = type;
    }

    public DamageInfo(Dictionary dict)
    {
        foreach (var prop in GetPropertyList())
        {
            var name = prop["name"].AsString();
            var flags = prop["usage"].As<PropertyUsageFlags>();
            if (flags.HasFlag(PropertyUsageFlags.ScriptVariable))
                Set(name, dict[name]);
        }
    }

    public static Dictionary ToNetwork(DamageInfo info)
    {
        var dict = new Dictionary();
        if (IsInstanceValid(info))
        {
            foreach (var prop in info.GetPropertyList())
            {
                var name = prop["name"].AsString();
                var flags = prop["usage"].As<PropertyUsageFlags>();
                if (flags.HasFlag(PropertyUsageFlags.ScriptVariable))
                    dict[name] = info.Get(name);
                // GD.PrintS(name, flags, flags.HasFlag(PropertyUsageFlags.ScriptVariable));
            }
        }
        return dict;
    }

    public override string ToString()
    {
        return $"DamageInfo({Amount}, {Source?.AbsolutePath ?? "null"}, {TypeOfDamage})";
    }
}
