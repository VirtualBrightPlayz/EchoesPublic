using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NameOverrideAttribute : Attribute
{
    public string Name { get; }

    public NameOverrideAttribute(string name)
    {
        this.Name = name;
    }
}
