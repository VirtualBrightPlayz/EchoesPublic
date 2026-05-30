using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TooltipAttribute : Attribute
{
    public TooltipAttribute(string tooltip)
    {
        Tooltip = tooltip;
    }

    public string Tooltip { get; }
}