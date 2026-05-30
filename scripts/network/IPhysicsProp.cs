using System;
using Godot;

public interface IPhysicsProp : IGrabbable
{
    [Export]
    public AffectedBy Affected { get; set;  }
    
    [Flags]
    public enum AffectedBy : byte
    {
        PlayerPush = 2,
        Bullet = 4,
        Explosion = 8,
        Environment = 16,
    }

    public void OnImpulse(Vector3 vel, Vector3 pos);
}

