using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ActiveAttribute : Attribute
{
    public bool Active { get; }

    public ActiveAttribute(bool active)
    {
        Active = active;
    }
}