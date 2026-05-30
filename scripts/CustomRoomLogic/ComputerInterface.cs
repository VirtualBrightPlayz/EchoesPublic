using Godot;

[GlobalClass]
public partial class ComputerInterface : StaticBody3D, IInteractable
{
	[Export]
	public WorldCanvas canvas;
	[Export]
	public Vector2 mousePosition;
	[Export]
	public bool isUsing = false;

	public ButtonInputFlags leftFlags;
	public Vector2 mouseMotion;

	public Vector3 WorldInteractPosition => GlobalPosition;

	public IInteractable.InteractType ActionType => IInteractable.InteractType.Touch;

	public override void _Input(InputEvent ev)
	{
		if (IsInstanceValid(NetworkPlayer.LocalInstance) && InputManager.Instance.CurrentSet.ResourceName == "MenuLocked" && isUsing)
		{
			if (ev is InputEventMouseMotion motion)
			{
				mouseMotion += motion.ScreenRelative;
			}
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (IsInstanceValid(NetworkPlayer.LocalInstance) && InputManager.Instance.CurrentSet.ResourceName == "MenuLocked" && isUsing)
		{
			if (InputManager.Instance.GetDigitalActionData(LocalPlayerInput.ExitLockMenu))
			{
				InputManager.Instance.ChangeActionSet("InGame");
			}
			else
			{
				InputManager.UpdateInput(InputManager.Instance, LocalPlayerInput.ClickLockMenu, ref leftFlags);
				Vector2 motion = mouseMotion - InputManager.Instance.GetStickPadActionData(LocalPlayerInput.CameraLockMenu);
				mouseMotion = Vector2.Zero;
				if (leftFlags.HasFlag(ButtonInputFlags.JustPressed) || leftFlags.HasFlag(ButtonInputFlags.JustReleased))
				{
					var input = new InputEventMouseButton();
					input.ButtonIndex = MouseButton.Left;
					input.ButtonMask = leftFlags.HasFlag(ButtonInputFlags.Pressed) ? MouseButtonMask.Left : 0;
					input.Pressed = leftFlags.HasFlag(ButtonInputFlags.Pressed);
					input.Position = mousePosition;
					input.GlobalPosition = input.Position;
					canvas.PushInput(input);
				}
				if (!motion.IsZeroApprox())
				{
					mousePosition = (mousePosition + motion).Clamp(Vector2.Zero, canvas.viewport.Size);
					var input = new InputEventMouseMotion();
					input.ButtonMask = leftFlags.HasFlag(ButtonInputFlags.Pressed) ? MouseButtonMask.Left : 0;
					input.Position = mousePosition;
					input.GlobalPosition = input.Position;
					input.Relative = motion;
					input.ScreenRelative = motion;
					canvas.PushInput(input);
				}
			}
		}
		else
		{
			// isUsing = false;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (IsInstanceValid(NetworkPlayer.LocalInstance) && InputManager.Instance.CurrentSet.ResourceName == "MenuLocked" && isUsing)
		{
		}
		else
		{
			isUsing = false;
		}
	}

	public bool CanUse(IItemHolder holder, ItemObject item)
	{
		return holder.GetPlayer().TryGetAbility(out InventoryAbility _);
	}

	public void Use(IItemHolder holder, ItemObject item)
	{
		isUsing = true;
		leftFlags = 0;
		InputManager.Instance.ChangeActionSet("MenuLocked");
	}

	public void UseEnd(IItemHolder holder, ItemObject item)
	{
	}
}
