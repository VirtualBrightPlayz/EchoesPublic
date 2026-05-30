using System;

[AttributeUsage(AttributeTargets.Class)]
public class HiddenCommand : Attribute
{
    public HiddenCommand()
    { }
}
