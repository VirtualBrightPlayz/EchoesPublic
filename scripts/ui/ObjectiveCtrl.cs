using Godot;
using System;
using System.Collections.Generic;

public partial class ObjectiveCtrl : Node
{
    [Export]
    public Label template;

    public List<Label> labels = new List<Label>();

    public override void _Ready()
    {
        base._Ready();
        Update();
    }

    public void Update()
    {
        var parent = GetParent();
        foreach (var label in labels)
        {
            label.QueueFree();
        }
        labels.Clear();
        if (IsInstanceValid(ObjectivePointManager.Instance))
        {
            foreach (var objective in ObjectivePointManager.Instance.activeObjectives)
            {
                Label label = (Label)template.Duplicate();
                labels.Add(label);
                label.Text = objective;
                label.Visible = true;
                parent.AddChild(label);
            }
        }
        foreach (var objective in NetworkPlayer.LocalInstance.objectives)
        {
            Label label = (Label)template.Duplicate();
            labels.Add(label);
            label.Text = objective;
            label.Visible = true;
            parent.AddChild(label);
        }
    }
}
