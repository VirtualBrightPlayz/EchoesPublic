using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class StepAttribute : Attribute
{
    public double Step { get; }

    public StepAttribute(double step)
    {
        Step = step;
    }
}