using DeBroglie;
using DeBroglie.Constraints;
using Godot;
using System;

public partial class ConnectionValidator : Resource
{
    public virtual bool IsConnectionValid(Layout layout, LayoutCell cell)
    {
        return false;
    }
}
