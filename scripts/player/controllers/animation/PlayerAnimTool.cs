using Godot;
using System;

[Tool]
[GlobalClass]
public partial class PlayerAnimTool : Node
{

    [ExportToolButton("Setup")]
    public Callable SetupCall => Callable.From(Create);

    [Export]
    public AnimationTree animTree;
    [Export]
    public Skeleton3D skeleton;
    [Export]
    [ExportGroup("Leg Bones", nameof(LegBones))]
    public int LegBonesCount
    {
        get => LegBones.Count;
        set
        {
            LegBones.Resize(value);
            NotifyPropertyListChanged();
        }
    }
    public Godot.Collections.Array<string> LegBones = new Godot.Collections.Array<string>();

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        if (IsInstanceValid(skeleton))
        {
            var arr = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            for (int i = 0; i < LegBonesCount; i++)
            {
                arr.Add(new()
                {
                    { "name", nameof(LegBones) + i.ToString() },
                    { "type", (long)Variant.Type.String },
                    { "hint", (long)PropertyHint.Enum },
                    { "hint_string", skeleton.GetConcatenatedBoneNames() },
                });
            }
            return arr;
        }
        return base._GetPropertyList();
    }

    public override Variant _Get(StringName property)
    {
        string name = property.ToString().TrimPrefix(nameof(LegBones));
        if (name != property.ToString() && int.TryParse(name, out int i))
        {
            return LegBones[i];
        }
        return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        string name = property.ToString().TrimPrefix(nameof(LegBones));
        if (name != property.ToString() && int.TryParse(name, out int i))
        {
            LegBones[i] = value.AsString();
            return true;
        }
        return base._Set(property, value);
    }

    public AnimationNodeAnimation LoadAnim(StringName name, bool reverse = false)
    {
        return new AnimationNodeAnimation()
        {
            Animation = name,
            PlayMode = reverse ? AnimationNodeAnimation.PlayModeEnum.Backward : AnimationNodeAnimation.PlayModeEnum.Forward,
        };
    }

    public AnimationNodeAnimation LoadAnimLoop(StringName name, bool reverse = false)
    {
        return new AnimationNodeAnimation()
        {
            Animation = name,
            PlayMode = reverse ? AnimationNodeAnimation.PlayModeEnum.Backward : AnimationNodeAnimation.PlayModeEnum.Forward,
            LoopMode = Animation.LoopModeEnum.Linear,
            UseCustomTimeline = true,
            TimelineLength = animTree.GetNode<AnimationPlayer>(animTree.AnimPlayer).GetAnimation(name).Length,
            ResourceName = name,
        };
    }

    public AnimationNodeBlendSpace2D CreateBlend2D(string name, StringName idle, StringName front, StringName back, StringName left, StringName right)
    {
        var blend = new AnimationNodeBlendSpace2D();
        blend.ResourceName = name;
        blend.AddBlendPoint(LoadAnim(idle), Vector2.Zero);
        blend.AddBlendPoint(LoadAnim(front), Vector2.Up);
        blend.AddBlendPoint(LoadAnim(back), Vector2.Down);
        blend.AddBlendPoint(LoadAnim(left), Vector2.Left);
        blend.AddBlendPoint(LoadAnim(right), Vector2.Right);
        return blend;
    }

    public AnimationNodeBlendSpace2D CreateWalkBlend2D(string name, StringName idle, StringName front, StringName back, StringName left, StringName right)
    {
        var blend = new AnimationNodeBlendSpace2D();
        blend.ResourceName = name;
        blend.AddBlendPoint(LoadAnim(idle), Vector2.Zero);
        blend.AddBlendPoint(LoadAnim(front), Vector2.Up);
        blend.AddBlendPoint(LoadAnim(back, true), Vector2.Down);
        blend.AddBlendPoint(LoadAnim(left), Vector2.Left);
        blend.AddBlendPoint(LoadAnim(right), Vector2.Right);
        return blend;
    }

    public AnimationNodeBlendTree CreateBlendTree(string name, AnimationRootNode node1, AnimationRootNode node2)
    {
        var blend = new AnimationNodeBlendTree();
        blend.ResourceName = name;
        var blend2 = new AnimationNodeBlend2();
        blend2.ResourceName = "MainBlend";
        blend2.FilterEnabled = true;
        for (int i = 0; i < LegBones.Count; i++)
        {
            if (skeleton.FindBone(LegBones[i]) == -1)
                continue;
            var path = $"{animTree.GetNode(animTree.RootNode).GetPathTo(skeleton, true)}:{LegBones[i]}";
            // if (skeleton.UniqueNameInOwner)
                // path = $"%{skeleton.Name}:{LegBones[i]}";
            GD.Print(path);
            blend2.SetFilterPath(path, true);
        }
        blend.AddNode(blend2.ResourceName, blend2);
        blend.AddNode(node1.ResourceName, node1);
        blend.AddNode(node2.ResourceName, node2);
        blend.ConnectNode("output", 0, blend2.ResourceName);
        blend.ConnectNode(blend2.ResourceName, 0, node1.ResourceName);
        blend.ConnectNode(blend2.ResourceName, 1, node2.ResourceName);
        blend2.SetParameter("blend_amount", 1f);
        return blend;
    }

    public AnimationNodeStateMachine CreateLegsStateMachine()
    {
        var stateMachine = new AnimationNodeStateMachine();
        stateMachine.ResourceName = "Legs";
        stateMachine.StateMachineType = AnimationNodeStateMachine.StateMachineTypeEnum.Nested;
        stateMachine.AddNode("Walk", CreateWalkBlend2D("Walk", "basic/idle", "basic/walking", "basic/walking", "basic/left", "basic/right"));
        stateMachine.AddTransition("Start", "Walk", new AnimationNodeStateMachineTransition()
        {
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        // jump
        stateMachine.AddNode("Jump", LoadAnim("basic/jump"));
        stateMachine.AddTransition("Walk", "Jump", new AnimationNodeStateMachineTransition()
        {
            XfadeTime = 0.1f,
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Enabled,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        stateMachine.AddTransition("Jump", "Walk", new AnimationNodeStateMachineTransition()
        {
            XfadeTime = 0.1f,
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Enabled,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        return stateMachine;
    }

    public AnimationNodeStateMachine CreateArmsStateMachine()
    {
        var stateMachine = new AnimationNodeStateMachine();
        stateMachine.ResourceName = "Arms";
        stateMachine.StateMachineType = AnimationNodeStateMachine.StateMachineTypeEnum.Nested;
        stateMachine.AddNode("Walk", CreateWalkBlend2D("Walk", "basic/idle", "basic/walking", "basic/walking", "basic/left", "basic/right"));
        stateMachine.AddTransition("Start", "Walk", new AnimationNodeStateMachineTransition()
        {
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        // pistol
        stateMachine.AddNode("Pistol", CreateWalkBlend2D("Pistol", "pistol/pistol_idle", "pistol/pistol_walk", "pistol/pistol_walk", "pistol/walk_left", "pistol/walk_right"));
        stateMachine.AddTransition("Walk", "Pistol", new AnimationNodeStateMachineTransition()
        {
            XfadeTime = 0.1f,
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Enabled,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        stateMachine.AddTransition("Pistol", "Walk", new AnimationNodeStateMachineTransition()
        {
            XfadeTime = 0.1f,
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Enabled,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        // rifle
        stateMachine.AddNode("Rifle", CreateWalkBlend2D("Rifle", "rifle/rifle_aiming_idle", "rifle/walking", "rifle/walking", "rifle/strafe_left", "rifle/strafe_right"));
        stateMachine.AddTransition("Walk", "Rifle", new AnimationNodeStateMachineTransition()
        {
            XfadeTime = 0.1f,
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Enabled,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        stateMachine.AddTransition("Rifle", "Walk", new AnimationNodeStateMachineTransition()
        {
            XfadeTime = 0.1f,
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Enabled,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        // small item
        stateMachine.AddNode("SmallItem", LoadAnim("basic/small_item"));
        stateMachine.AddTransition("Walk", "SmallItem", new AnimationNodeStateMachineTransition()
        {
            XfadeTime = 0.1f,
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Enabled,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        stateMachine.AddTransition("SmallItem", "Walk", new AnimationNodeStateMachineTransition()
        {
            XfadeTime = 0.1f,
            AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Enabled,
            SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate,
        });
        return stateMachine;
    }

    public void Create()
    {
        animTree.TreeRoot = CreateBlendTree("Root", CreateArmsStateMachine(), CreateLegsStateMachine());
    }

    public AnimationNodeBlendTree CreateBlendTreeSimple(string name, AnimationNode node1, AnimationNode node2)
    {
        var blend = new AnimationNodeBlendTree();
        blend.ResourceName = name;
        var blend2 = new AnimationNodeBlend2();
        blend2.ResourceName = "MainBlend";
        blend.AddNode(blend2.ResourceName, blend2);
        blend.AddNode(node1.ResourceName, node1);
        blend.AddNode(node2.ResourceName, node2);
        blend.ConnectNode("output", 0, blend2.ResourceName);
        blend.ConnectNode(blend2.ResourceName, 0, node1.ResourceName);
        blend.ConnectNode(blend2.ResourceName, 1, node2.ResourceName);
        blend2.SetParameter("blend_amount", 0f);
        return blend;
    }

    public void CreateSimple(StringName idle, StringName walk)
    {
        animTree.TreeRoot = CreateBlendTreeSimple("Root", LoadAnimLoop(idle), LoadAnimLoop(walk));
    }
}
