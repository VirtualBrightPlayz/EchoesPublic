namespace Godot;

public interface IFlammable
{
    public FireWorldEffects FireEffect { get; set; }
    
    public float Amount { get; set;  }
    
    public bool OnFire { get; }

    public float Flammability { get; set; }
    
    public void Ignite(float duration);
    
    public void Extinguish();
}