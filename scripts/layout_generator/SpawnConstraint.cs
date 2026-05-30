using DeBroglie;
using DeBroglie.Constraints;
using Godot;
using System;

public partial class SpawnConstraint : Resource, ITileConstraint
{
    public virtual void Check(TilePropagator propagator)
    {
        throw new NotImplementedException();
    }

    public virtual void Init(TilePropagator propagator)
    {
        throw new NotImplementedException();
    }
}
