using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class AdminCategoryProps : Node
{
	public AdminHUD Admin => GetParent().GetMeta(AdminHUD.META_NAME).As<AdminHUD>();

	[Export]
	public Control container;

	public override void _Ready()
	{
		List<PackedScene> props = new List<PackedScene>();
		if (!IsInstanceValid(ItemManager.Instance))
		{
			Log.PrintErr("Item manager is not valid");
			return;
		}
		else
		{
			props = ItemManager.Instance.Data.Props.ToList();
		}
		List<Button> btns = new List<Button>();
		for (int i = 0; i < props.Count; i++)
		{
			int j = i;
			var btn = new Button();
			btn.Text = props[i].ResourcePath.Split('/').Last().Replace(".tscn", "");
			Node3D prop = props[i].Instantiate() as Node3D;
			PhysicsProp3D physProp = prop as PhysicsProp3D;
			if (prop is not PhysicsProp3D)
			{
				if (!IsInstanceValid(prop))
				{
					Log.PrintWarn("Invalid prop: " + props[i].ResourcePath);
					continue;
				}
				Array<Node> pr = prop.FindChildren("*", string.Empty, true, false);
				foreach (Node child in pr)
				{
					if (child is PhysicsProp3D prop3d)
					{
						if (!GodotObject.IsInstanceValid(prop3d.Parent))
						{
							physProp = prop3d;
							break;
						}
					}
				}
			}
			if (!IsInstanceValid(physProp))
			{
				Log.PrintWarn("Prop is invalid for admin category: " + props[i].ResourcePath);
				continue;
			}
			btn.SetMeta("category", physProp.Category.ToString());
			//Log.Print($"Object:  {props[i].ResourcePath}, Category: {physProp.Category.ToString()}");
			//container.AddChild(btn);
			btn.Pressed += () => 
				Admin.RunWithSelectedPlayers("spawnprop {0} " + j);
			btns.Add(btn);
			prop.QueueFree();
		}
		btns.Sort((x, y) => x.Text.CompareTo(y.Text));
		List<PropCategory> cats = Enum.GetValues<PropCategory>().ToList();
		cats.Sort((x, y) => x.ToString().CompareTo(y.ToString()));
		foreach (PropCategory cat in cats)
		{
			HSeparator separator = new HSeparator();
			separator.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			container.AddChild(separator);
			Label label = new Label();
			label.Text = cat.ToString().Replace("_", " ");
			label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			container.AddChild(label);
			HSeparator separator2 = new HSeparator();
			container.AddChild(separator2);
			HFlowContainer box = new HFlowContainer();
			box.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			container.AddChild(box);
			foreach (var btn in btns.Where(btn => btn.GetMeta("category").As<string>() == cat.ToString()))
			{
				box.AddChild(btn);
			}
		}
	}
}
