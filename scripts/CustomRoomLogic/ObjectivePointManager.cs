using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class ObjectivePointManager : SingletonNode3D<ObjectivePointManager>
{
    public List<ObjectivePoint> Points = new List<ObjectivePoint>();

    [Signal]
    public delegate void OnObjectiveDoneEventHandler(string key);

    [Export]
    public Godot.Collections.Array<string> activeObjectives = new Godot.Collections.Array<string>();
    [Export]
    public Godot.Collections.Array<string> completeObjectives = new Godot.Collections.Array<string>();

    public bool CompleteObjective(ObjectiveAsset objective)
    {
        if (activeObjectives.Remove(objective.key))
        {
            completeObjectives.Add(objective.key);
            return true;
        }
        return false;
    }
}