using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class UseCustomSceneAttribute : Attribute
{
    private string _path = null;

    public string Path => _path;

    public UseCustomSceneAttribute(string path)
    {
        _path = path;
    }
}
