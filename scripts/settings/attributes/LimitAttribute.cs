using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LimitAttribute : Attribute
{
    public LimitAttribute(double min, double max)
    {
        DoubleMinValue = min;
        DoubleMaxValue = max;
        IntMinValue = (int)min;
        IntMaxValue = (int)max;
    }

    public LimitAttribute(int min, int max)
    {
        IntMinValue = min;
        IntMaxValue = max;
        DoubleMinValue = (double)min;
        DoubleMaxValue = (double)max;
    }

    public double DoubleMinValue { get; }

    public double DoubleMaxValue { get; }

    public int IntMinValue { get; }

    public int IntMaxValue { get; }
}
