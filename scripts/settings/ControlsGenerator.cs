using Godot;

[GlobalClass]
[Tool]
public partial class ControlsGenerator : Node
{
    [ExportToolButton("Make UI")]
    public Callable MakeUICall => Callable.From(MakeUI);

    [Export]
    public SettingsGenerator SettingsGenerator { get; set; }

    [Export]
    public ControlsUIController ControlsUIController { get; set; }

    [Export]
    public PackedScene ControlScene { get; set; }

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Engine.IsEditorHint())
        {
            if (IsInstanceValid(SettingsGenerator))
            {
                //SettingsGenerator.UICreated += SettingsGenerator_UICreated;
            }
        }
        else
        {
            BuildInputMapControls();
        }
    }

    private void SettingsGenerator_UICreated()
    {
        MakeUI();
    }

    private void MakeUI()
    {
        //BuildInputMapControls();
    }

    private void BuildInputMapControls()
    {
        HSeparator separator = new HSeparator();
        separator.Set(HSeparator.PropertyName.SizeFlagsHorizontal, (long)Control.SizeFlags.ExpandFill);
        Label label = new Label();
        label.Set(Label.PropertyName.SizeFlagsHorizontal, (long)Control.SizeFlags.ExpandFill);
        HSeparator separator2 = new HSeparator();
        separator2.Set(HSeparator.PropertyName.SizeFlagsHorizontal, (long)Control.SizeFlags.ExpandFill);
        SettingsGenerator.SettingsBox.AddChild(separator);
        separator.Set(Node.PropertyName.Owner, SettingsGenerator.SettingsBox.Owner);
        SettingsGenerator.SettingsBox.AddChild(label);
        label.Set(Node.PropertyName.Owner, SettingsGenerator.SettingsBox.Owner);
        label.Set(Label.PropertyName.Text, "Controls");
        label.Set(Node.PropertyName.Name, "Controls");
        SettingsGenerator.SettingsBox.AddChild(separator2);
        separator2.Set(Node.PropertyName.Owner, SettingsGenerator.SettingsBox.Owner);
        foreach (InputSettings.InputMapping mapping in InputSettings.Mappings)
        {
            Node sceneRoot = ControlScene.Instantiate();
            SetEditableInstance(sceneRoot, true);
            KeyInput control = null;
            if (sceneRoot is not KeyInput ctrl)
            {
                Log.PrintWarn(sceneRoot.GetType().FullName);
                foreach (Node child in sceneRoot.FindChildren("*", string.Empty, true, false))
                {
                    Log.PrintWarn(child.GetType().FullName);
                    if (child is KeyInput childCtrl)
                    {
                        control = childCtrl;
                        break;
                    }
                }
            }
            else
            {
                control = ctrl;
            }
            SettingsGenerator.RecMakeOwner(sceneRoot, GetTree().EditedSceneRoot);
            sceneRoot.Set(Node.PropertyName.Owner, GetTree().EditedSceneRoot);
            control.Set(KeyInput.PropertyName.InputActionName, mapping.Action);
            control.Set(KeyInput.PropertyName.InputActionType, mapping.Type);
            control.Set(KeyInput.PropertyName.Controller, ControlsUIController);
            control.Label.Set(Label.PropertyName.Text, mapping.Action.ToUpper());
            control.Label.Set(Label.PropertyName.HorizontalAlignment, (int)HorizontalAlignment.Right);
            sceneRoot.Set(Control.PropertyName.SizeFlagsHorizontal, (ulong)Control.SizeFlags.ExpandFill);
            sceneRoot.Set(Node.PropertyName.Name, mapping.Action);
            SettingsGenerator.SettingsBox.AddChild(sceneRoot);
            ControlsUIController.Inputs.Add(control);
            ControlsUIController.SettingsUIController.SettingsControls.Add(control);
            control.Load();
        }
    }
}
