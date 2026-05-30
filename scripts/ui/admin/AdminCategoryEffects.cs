using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

public partial class AdminCategoryEffects : Node
{
    public AdminHUD Admin => GetParent().GetMeta(AdminHUD.META_NAME).As<AdminHUD>();
    
    [Export]
    public Control container;

    [Export]
    public Button clearEffectsButton;
    
    public override void _EnterTree()
    {
        base._EnterTree();
        clearEffectsButton.Pressed += () =>
        {
            Admin.RunWithSelectedPlayers("cleareffects {0}");
        };
        List<StatusEffectBase> effects = new List<StatusEffectBase>();
        if (!IsInstanceValid(ItemManager.Instance))
        {
            Log.PrintErr("Item manager is not valid");
            return;
        }
        else
        {
            effects = ItemManager.Instance.Data.StatusEffects.ToList();
        }
        effects.Sort((x, y) => x.Type - y.Type);
        foreach (var effect in effects)
        {
            GridContainer gridContainer = new GridContainer();
            gridContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            gridContainer.Columns = 5;
            Label effectName = new Label();
            effectName.Text = effect.Type.ToString();
            effectName.SetMeta("TYPE", (int)effect.Type);
            effectName.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            LineEdit effectDuration = new LineEdit();
            effectDuration.Text = effect.Duration.ToString(CultureInfo.InvariantCulture);
            effectDuration.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            LineEdit effectIntensity = new LineEdit();
            effectIntensity.Text = effect.Intensity.ToString(CultureInfo.InvariantCulture);
            effectIntensity.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            Button applyButton = new Button();
            applyButton.Text = "Apply";
            applyButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            applyButton.Pressed += () =>
            {
                EffectType effectType = (EffectType)effectName.GetMeta("TYPE").As<int>();
                if (!double.TryParse(effectDuration.Text, out double duration))
                {
                    Log.PrintErr("Duration is invalid!");
                    return;
                }
                if (!double.TryParse(effectIntensity.Text, out double intensity))
                {
                    Log.PrintErr("Intensity is invalid!");
                    return;
                }
                Admin.RunWithSelectedPlayers("effect {0} " + $"{effectType} {duration} {intensity}");
            };
            Button removeButton = new Button();
            removeButton.Text = "Remove";
            removeButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            removeButton.Pressed += () =>
            {
                EffectType effectType = (EffectType)effectName.GetMeta("TYPE").As<int>();
                if (!double.TryParse(effectDuration.Text, out double duration))
                {
                    Log.PrintErr("Duration is invalid!");
                    return;
                }
                if (!double.TryParse(effectIntensity.Text, out double intensity))
                {
                    Log.PrintErr("Intensity is invalid!");
                    return;
                }
                Admin.RunWithSelectedPlayers("effect {0} " + $"{effectType} 0 0");
            };
            gridContainer.AddChild(effectName);
            gridContainer.AddChild(effectDuration);
            gridContainer.AddChild(effectIntensity);
            gridContainer.AddChild(applyButton);
            gridContainer.AddChild(removeButton);
            container.AddChild(gridContainer);
        }
    }
}