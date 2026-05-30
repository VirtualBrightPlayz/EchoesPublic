using Godot;
using System.Collections.Generic;

public abstract class Manager<R, C, K> where R : Resource where K : ManagerRegistrationConfiguration
{
    public abstract void Register(R resource, C data, K config);

    public abstract void Unregister(R resource);

    public abstract IEnumerable<C> All { get; }


}

public class ManagerRegistrationConfiguration
{

}