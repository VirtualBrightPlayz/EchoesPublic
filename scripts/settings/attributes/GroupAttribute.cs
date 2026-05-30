using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GroupAttribute : Attribute
{
    public string GroupName { get; }
    
    public GroupAttribute(string groupName)
    {
        GroupName = groupName;
    }
}