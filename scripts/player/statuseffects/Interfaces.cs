using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IMovementSpeedModifier
{
    public bool SpeedModifierActive { get; }

    public float SpeedModifier { get; }
}

public interface IAccelModifier
{
    public bool AccelModifierActive { get; }

    public float AccelModifier { get; }
}

public interface ISprintModifier
{
    public bool SprintModifierActive { get; }

    public float SprintModifier { get; }

    public bool AllowSneaking { get; }
}

public interface ISneakModifier
{
    public bool SneakModifierActive { get; }

    public float SneakModifier { get; }

    /// <summary>
    /// Doesn't do anything cuz im lazy (for now)
    /// </summary>
    public bool AllowSneaking { get; }
}

public interface IJumpModifier
{
    public bool JumpModifierActive { get; }

    public float JumpModifier { get; }

    /// <summary>
    /// Doesn't do anything cuz im lazy (for now)
    /// </summary>
    public bool AllowJumping { get; }
}

public interface IGravityModifier
{
    public bool GravityModifierActive { get; }

    public float GravityModifier { get; }
}

public interface IStaminaDecreaseModifier
{
    public bool StaminaDecreaseModifierActive { get; }

    public float StaminaDecreaseModifier { get; }
}

public interface IStaminaIncreaseModifier
{
    public bool StaminaIncreaseModifierActive { get; }

    public float StaminaIncreaseModifier { get; }
}

public interface IStaminaPauseTimerModifier
{
    public bool StaminaPauseModifierActive { get; }

    public float StaminaPauseModifier { get; }
}

public interface IMaxStaminaModifier
{
    public bool MaxStaminaModifierActive { get; }

    public float MaxStaminaModifier { get; }
}

public interface IDamageModifier
{
    public bool ActiveForType(DamageType damageType);

    public float DamageMultiplierForType(DamageType damageType);
}

public interface IHealingModifier
{
    public bool HealingModifierActiveForSource(IHealSource healSource);

    public float HealingMultiplierForSource(IHealSource healSource);
}

public interface IFOVModifier
{
    public bool FOVModifierActive { get; }
    
    public float FOVModifier { get; }
}
