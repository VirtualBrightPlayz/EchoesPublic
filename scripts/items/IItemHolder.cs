using Godot;

public interface IItemHolder
{
    // bool HasAuthority { get; }
    // bool IsServer { get; }
    bool IsSocket { get; }
    ButtonInputFlags InputPrimary { get; }
    CollisionObject3D MainCollider { get; }
    Transform3D HolderTransform { get; }
    Transform3D AimTransform { get; }
    bool IsAimingDown { get; }
    BasePlayer GetPlayer();
    NodePath GetPath();
}