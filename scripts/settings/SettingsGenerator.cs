using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[GlobalClass]
[Tool]
public partial class SettingsGenerator : Node
{
    [ExportToolButton("Remake UI")]
    public Callable RemakeUICall => Callable.From(RemakeUI);

    [ExportToolButton("Make UI")]
    public Callable MakeUICall => Callable.From(MakeUI);

    [ExportToolButton("Destroy UI")]
    public Callable DestroyUICall => Callable.From(DestroyUI);

    [Export]
    public VBoxContainer SettingsBox { get; set; }

    [Export]
    public Node Controller { get; set; }

    public SettingsUIController UIController
    {
        get
        {
            return Controller as SettingsUIController;
        }
    }

    [Export]
    public Godot.Collections.Dictionary<ControlType, PackedScene> TypeToScene { get; set; }

    [Export]
    public Godot.Collections.Dictionary<HardwareDetectorType, PackedScene> HardwareTypeToScene { get; set; }

    [Export]
    public Godot.Collections.Array<string> LegalTypes { get; set; } = new Array<string>()
    {
        typeof(string).FullName,
        typeof(double).FullName,
        typeof(float).FullName,
        typeof(int).FullName,
        typeof(long).FullName,
        typeof(short).FullName,
        typeof(ushort).FullName,
        typeof(uint).FullName,
        typeof(ulong).FullName,
        typeof(bool).FullName,
        typeof(Enum).FullName,
    };

    [Export]
    public Godot.Collections.Dictionary<string, ControlType> TypeStringToControlType { get; set; } = new Godot.Collections.Dictionary<string, ControlType>()
    {
        [typeof(string).FullName] = ControlType.Text,
        [typeof(double).FullName] = ControlType.Slider,
        [typeof(float).FullName] = ControlType.Slider,
        [typeof(int).FullName] = ControlType.Slider,
        [typeof(long).FullName] = ControlType.Slider,
        [typeof(short).FullName] = ControlType.Slider,
        [typeof(ushort).FullName] = ControlType.Slider,
        [typeof(uint).FullName] = ControlType.Slider,
        [typeof(ulong).FullName] = ControlType.Slider,
        [typeof(bool).FullName] = ControlType.Checkbox,
        [typeof(Enum).FullName] = ControlType.Dropdown,
    };

    public event Action UICreated;

    public struct SettingsGeneratorTarget
    {
        public Type Target;
        public object TargetObject;
    }

    public List<SettingsGeneratorTarget> Targets = new List<SettingsGeneratorTarget>()
    {
        new SettingsGeneratorTarget()
        {
            Target = typeof(Settings.UserSettings),
            TargetObject = Settings.User,
        }
    };

    public static Type Target { get; private set; } = typeof(Settings.UserSettings);

    public static object TargetObject { get; private set; } = Settings.User;

    private void RemakeUI()
    {
        DestroyUI();
        MakeUI();
    }

    private void MakeUI()
    {
        foreach (var target in Targets)
        {
            Target = target.Target;
            TargetObject = target.TargetObject;
            BuildTarget();
        }
        UICreated?.Invoke();
    }

    private void BuildTarget()
    {
        List<Type> legalTypes = new List<Type>();
        foreach (string type in LegalTypes)
        {
            Type t = Type.GetType(type);
            if (t == null)
            {
                Log.PrintErr($"Type '{type}' not found.");
                continue;
            }
            legalTypes.Add(t);
        }
        List<Setting> generated = new List<Setting>();
        string currentGroup = null;
        foreach (PropertyInfo prop in Target.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetCustomAttribute<HiddenAttribute>() == null && x.CanWrite && x.CanRead))
        {
            Log.Print($"Processing: {prop.Name}");
            GroupAttribute groupAttr = prop.GetCustomAttribute<GroupAttribute>();
            if (groupAttr != null)
            {
                currentGroup = groupAttr.GroupName;
            }
            Setting s = GenerateSettingFromProperty(prop, currentGroup);
            if (s == null)
            {
                Log.PrintErr($"Generated null setting for property {prop.Name}");
                continue;
            }
            generated.Add(s);
        }
        currentGroup = null;
        foreach (FieldInfo field in Target.GetFields(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetCustomAttribute<HiddenAttribute>() == null))
        {
            Log.Print($"Processing: {field.Name}");
            GroupAttribute groupAttr = field.GetCustomAttribute<GroupAttribute>();
            if (groupAttr != null)
            {
                currentGroup = groupAttr.GroupName;
            }
            Setting s = GenerateSettingFromField(field, currentGroup);
            if (s == null)
            {
                Log.Print($"Generated null setting for field {field.Name}");
                continue;
            }
            generated.Add(s);
        }
        Godot.Collections.Array<BaseSettingsControl> settings = new Godot.Collections.Array<BaseSettingsControl>();
        foreach (Setting setting in generated)
        {
            var bsc = CreateOrGetControl(setting, out var baseControl);
            if (bsc == null)
            {
                Log.PrintErr($"Failed to generate control for: {setting.Name}");
                continue;
            }
            settings.Add(baseControl);
            AddElementToUI(bsc, setting);
            baseControl.EditorOnElementCreated();
        }
        Controller.Set(SettingsUIController.PropertyName.SettingsControls, settings);
#if TOOLS
        EditorInterface.Singleton.MarkSceneAsUnsaved();
#endif
    }

    private void DestroyUI()
    {
        if (!Engine.IsEditorHint())
        {
            return;
        }
        foreach (Node n in UIController.SettingsControls)
        {
            n.QueueFreeNow();
        }
        UIController.Set(SettingsUIController.PropertyName.SettingsControls, new Control[0]);
        foreach (Node n in SettingsBox.FindChildren("*", string.Empty, true, false))
        {
            n.QueueFreeNow();
        }
#if TOOLS
        EditorInterface.Singleton.MarkSceneAsUnsaved();
#endif
    }

    private void CreateBaselineSettingData(ref Setting setting, MemberInfo info, string group = null)
    {
        NameOverrideAttribute nameOverride = info.GetCustomAttribute<NameOverrideAttribute>();
        if (nameOverride == null)
        {
            setting.Name = info.Name;
        }
        else
        {
            setting.Name = nameOverride.Name;
        }
        setting.Group = group;
        setting.SettingsObject = TargetObject;
        setting.Member = info;
        TooltipAttribute tooltip = info.GetCustomAttribute<TooltipAttribute>();
        if (tooltip != null)
        {
            setting.Tooltip = tooltip.Tooltip;
        }
        NameOverrideAttribute nameOverrideAttribute = info.GetCustomAttribute<NameOverrideAttribute>();
        if (nameOverrideAttribute != null)
        {
            setting.Name = nameOverrideAttribute.Name;
        }
    }

    private Setting GenerateSettingFromProperty(PropertyInfo prop, string group = null)
    {
        Setting setting = new Setting();
        CreateBaselineSettingData(ref setting, prop, group);
        if (prop.PropertyType.IsSubclassOf(typeof(Enum)))
        {
            if (Enum.GetUnderlyingType(prop.PropertyType) != typeof(long))
            {
                Log.PrintErr("Enum cannot be assigned to a long, all enums used in the settings system must inherit from long.");
                return null;
            }
            setting.ControlType = ControlType.Dropdown;
            return setting;
        }
        if (!TypeStringToControlType.ContainsKey(prop.PropertyType.FullName))
        {
            Log.PrintErr($"Type not found in dictionary: {prop.PropertyType.FullName}");
            return null;
        }
        setting.ControlType = TypeStringToControlType[prop.PropertyType.FullName];
        return setting;
    }

    private Setting GenerateSettingFromField(FieldInfo info, string group = null)
    {
        Setting setting = new Setting();
        CreateBaselineSettingData(ref setting, info, group);
        if (info.FieldType.IsSubclassOf(typeof(Enum)))
        {
            if (Enum.GetUnderlyingType(info.FieldType) != typeof(long))
            {
                Log.PrintErr("Enum cannot be assigned to a long, all enums used in the settings system must inherit from long.");
                return null;
            }
            setting.ControlType = ControlType.Dropdown;
            return setting;
        }
        if (!TypeStringToControlType.ContainsKey(info.FieldType.FullName))
        {
            Log.PrintErr($"Type not found in dictionary: {info.FieldType.FullName}");
            return null;
        }
        setting.ControlType = TypeStringToControlType[info.FieldType.FullName];
        return setting;
    }

    private Node CreateOrGetControl(Setting setting, out BaseSettingsControl baseControl)
    {
        PackedScene scene;
        if (!TypeToScene.ContainsKey(setting.ControlType))
        {
            Log.PrintWarn($"{setting.ControlType} not found in dict.");
            scene = TypeToScene[ControlType.Text];
        }
        else
        {
            scene = TypeToScene[setting.ControlType];
        }
        MemberInfo info = setting.Member;
        object defaultVal = null;
        if (info is PropertyInfo prop1)
        {
            defaultVal = prop1.GetValue(setting.SettingsObject);
        }
        else if (info is FieldInfo field)
        {
            defaultVal = field.GetValue(setting.SettingsObject);
        }
        UseHardwareDetectorAttribute hardwareDetector = info.GetCustomAttribute<UseHardwareDetectorAttribute>();
        if (hardwareDetector != null)
        {
            if (!HardwareTypeToScene.ContainsKey(hardwareDetector.DetectorType))
            {
                Log.PrintErr($"Unable to find hardware detector scene by type. Type: {hardwareDetector.DetectorType}.");
            }
            else
            {
                scene = HardwareTypeToScene[hardwareDetector.DetectorType];
            }
        }
        UseCustomSceneAttribute useCustomSceneAttribute = info.GetCustomAttribute<UseCustomSceneAttribute>();
        if (useCustomSceneAttribute != null)
        {
            if (!ResourceLoader.Exists(useCustomSceneAttribute.Path, typeHint: nameof(PackedScene)))
            {
                Log.PrintErr($"Can't load scene by path: {useCustomSceneAttribute.Path}, the default for this type has been used.");
            }
            else
            {
                PackedScene newScene = ResourceLoader.Load<PackedScene>(useCustomSceneAttribute.Path);
                scene = newScene;
            }
        }
        scene.ResourceLocalToScene = true;
        Node sceneRoot = scene.Instantiate(PackedScene.GenEditState.Instance);
        SetEditableInstance(sceneRoot, true);
        BaseSettingsControl control = null;
        if (sceneRoot is not BaseSettingsControl ctrl)
        {
            Log.PrintWarn(sceneRoot.GetType().FullName);
            foreach (Node child in sceneRoot.FindChildren("*", string.Empty, true, false))
            {
                Log.PrintWarn(child.GetType().FullName);
                if (child is BaseSettingsControl childCtrl)
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
        baseControl = control;
        if (control == null)
        {
            Log.PrintErr($"Unable to find BaseSettingControl within the scene {scene.ResourcePath}. Setting {setting.Name} has been ignored.");
            return null;
        }
        LimitAttribute limit = info.GetCustomAttribute<LimitAttribute>();
        StepAttribute step = info.GetCustomAttribute<StepAttribute>();
        TooltipAttribute tooltip = info.GetCustomAttribute<TooltipAttribute>();
        switch (setting.ControlType)
        {
            case ControlType.Slider:
                if (baseControl is CSlider slider)
                {
                    if (limit != null)
                    {
                        if (defaultVal.GetType().IsAssignableTo(typeof(double)))
                        {
                            double val = (double)defaultVal;
                            if (limit.DoubleMinValue > val)
                            {
                                Log.PrintWarn($"Setting {setting.Name} has a default value that is lower than the limit!");
                            }
                        }
                        slider.MinValue = limit.DoubleMinValue;
                        slider.MaxValue = limit.DoubleMaxValue;
                        //slider.Set(CSlider.PropertyName.MinValue, limit.DoubleMinValue);
                        //slider.Set(CSlider.PropertyName.MaxValue, limit.DoubleMaxValue);
                    }
                    if (step != null)
                    {
                        slider.Step = step.Step;
                        //slider.Set(CSlider.PropertyName.Step, step.Step);   
                    }
                }
                break;
            case ControlType.Text:
                if (baseControl is Text text)
                {
                    if (limit != null)
                    {
                        text.MaxLength = limit.IntMaxValue;
                        text.MinLength = limit.IntMinValue;
                    }
                }
                break;
            case ControlType.Dropdown:
                if (baseControl is Dropdown dropdown)
                {
                    Type resultType = null;
                    if (info is PropertyInfo prop)
                    {
                        resultType = prop.PropertyType;
                    }
                    else if (info is FieldInfo field)
                    {
                        resultType = field.FieldType;
                    }
                    if (resultType == null)
                    {
                        Log.PrintWarn($"Dropdown items could NOT be generated for setting {setting.Name}! Member is not a field or property.");
                    }
                    else
                    {
                        if (resultType.IsSubclassOf(typeof(Enum)))
                        {
                            string[] names = Enum.GetNames(resultType);
                            foreach (string name in names)
                            {
                                IConvertible value = (IConvertible)Enum.Parse(resultType, name);
                                dropdown.DropDown.AddItem(name, (int)((long)value));
                            }
                        }
                    }
                }
                break;
        }
        SettingsResource resource = new SettingsResource();
        resource.Name = setting.Name;
        resource.Group = setting.Group;
        resource.ObjectFullClass = typeof(Settings).FullName;
        resource.MemberName = setting.Member.Name;
        resource.ObjectPropertyName = nameof(Settings.User);
        resource.ControlType = setting.ControlType;
        resource.Tooltip = setting.Tooltip;
        baseControl.Set(BaseSettingsControl.PropertyName.Setting, resource);
        return sceneRoot;
    }

    private string LastCategory = "Misc.";

    private bool SeparatorCreated = false;

    private void AddElementToUI(Node controlRoot, Setting setting)
    {
        if (LastCategory != setting.Group)
        {
            if (setting.Group != null)
            {
                LastCategory = setting.Group;
                SeparatorCreated = false;
            }
        }
        if (!SeparatorCreated)
        {
            SeparatorCreated = true;
            HSeparator separator = new HSeparator();
            separator.Set(HSeparator.PropertyName.SizeFlagsHorizontal, (long)Control.SizeFlags.ExpandFill);
            //separator.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            Label label = new Label();
            label.Set(Label.PropertyName.SizeFlagsHorizontal, (long)Control.SizeFlags.ExpandFill);
            //label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            HSeparator separator2 = new HSeparator();
            separator2.Set(HSeparator.PropertyName.SizeFlagsHorizontal, (long)Control.SizeFlags.ExpandFill);
            //separator2.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            SettingsBox.AddChild(separator, true);
            separator.Set(Node.PropertyName.Owner, SettingsBox.Owner);
            SettingsBox.AddChild(label, true);
            label.Set(Node.PropertyName.Owner, SettingsBox.Owner);
            label.Set(Label.PropertyName.HorizontalAlignment, (int)HorizontalAlignment.Center);
            label.Set(Label.PropertyName.Text, LastCategory);
            label.Set(Node.PropertyName.Name, LastCategory);
            SettingsBox.AddChild(separator2, true);
            separator2.Set(Node.PropertyName.Owner, SettingsBox.Owner);
        }
        SettingsBox.AddChild(controlRoot, true);
        controlRoot.Set(Node.PropertyName.Owner, GetTree().EditedSceneRoot);
        SetEditableInstance(controlRoot, true);
        RecMakeOwner(controlRoot, GetTree().EditedSceneRoot);
        // controlRoot.Set(Control.PropertyName.SizeFlagsHorizontal, (ulong)Control.SizeFlags.ExpandFill);
        controlRoot.Set(Node.PropertyName.Name, setting.Member.Name);
    }

    public static void RecMakeOwner(Node n, Node owner)
    {
        n.Set(Node.PropertyName.Owner, owner);
        n.Set(Node.PropertyName.SceneFilePath, "");
        foreach (Node child in n.GetChildren())
        {
            RecMakeOwner(child, owner);
        }
    }
}