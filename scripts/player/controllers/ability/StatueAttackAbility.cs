using System;
using Godot;

[GlobalClass]
public partial class StatueAttackAbility : AttackAbility
{
    protected override bool _CanDamage(IHealth victim)
    {
        return Player.TryGetAbility(out StatueAbility ability) && (RoundManager.Instance.IsBlinking || !ability.seen);
    }
}