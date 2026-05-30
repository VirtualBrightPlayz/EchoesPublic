using Godot;

[GlobalClass]
public partial class WorldCanvas : MeshInstance3D
{
	public Vector2 MeshSize
	{
		get => ((QuadMesh)Mesh).Size;
		set => ((QuadMesh)Mesh).Size = value;
	}

	[Export]
	public SubViewport viewport;

	[Export]
	public bool input = false;

	public bool wasInputValid = false;

	InputEventMouseMotion Amotion = new InputEventMouseMotion();
	InputEventMouseButton Bmotion = new InputEventMouseButton();
	InputEventMouseMotion Cmotion = new InputEventMouseMotion();
	InputEventMouseButton Dmotion = new InputEventMouseButton();

	public bool HandleMouse(InputEvent ev, SubViewport view, Node3D mesh)
	{
		if (GetViewport().UseXR)
			return false;
		var invSize = MeshSize;
		invSize.Y *= -1f;
		var halfInvSize = MeshSize * 0.5f;
		halfInvSize.Y *= -1f;
		bool local = false;

		switch (ev)
		{
			//Mouse Move
			case InputEventMouseMotion mev:
				Vector3 Aorigin = GetViewport().GetCamera3D().ProjectRayOrigin(mev.Position);
				Vector3 Anormal = GetViewport().GetCamera3D().ProjectRayNormal(mev.Position);
				Plane Apl = new Plane(mesh.GlobalBasis.Z, mesh.GlobalPosition);
				Vector3 Av3 = Apl.IntersectsRay(Aorigin, Anormal) ?? Vector3.Zero;
				Av3 = mesh.GlobalTransform.AffineInverse() * Av3;
				Vector2 v2 = new Vector2(Av3.X, Av3.Y);
				v2 += halfInvSize;
				v2 /= invSize;
				if (v2 < Vector2.Zero || v2 > Vector2.One)
				{
					return false;
				}
				Amotion.ButtonMask = mev.ButtonMask;
				Amotion.Position = v2 * view.Size;
				Amotion.GlobalPosition = Amotion.Position;
				Amotion.Relative = mev.Relative;
				Amotion.Device = mev.Device;
				Amotion.WindowId = mev.WindowId;
				view.PushInput(Amotion, local);
				return true;

			//Mouse Click
			case InputEventMouseButton meb:
				Vector3 Borigin = GetViewport().GetCamera3D().ProjectRayOrigin(meb.Position);
				Vector3 Bnormal = GetViewport().GetCamera3D().ProjectRayNormal(meb.Position);
				Plane Bpl = new Plane(-mesh.GlobalBasis.Z, mesh.GlobalPosition);
				Vector3 Bv3 = Bpl.IntersectsRay(Borigin, Bnormal) ?? Vector3.Zero;
				Bv3 = mesh.GlobalTransform.AffineInverse() * Bv3;
				Vector2 Bv2 = new Vector2(Bv3.X, Bv3.Y);
				Bv2 += halfInvSize;
				Bv2 /= invSize;
				if (Bv2 < Vector2.Zero || Bv2 > Vector2.One)
				{
					return false;
				}
				Bmotion.ButtonIndex = meb.ButtonIndex;
				Bmotion.ButtonMask = meb.ButtonMask;
				Bmotion.Pressed = meb.Pressed;
				Bmotion.Position = Bv2 * view.Size;
				Bmotion.GlobalPosition = Bmotion.Position;
				Bmotion.Device = meb.Device;
				Bmotion.WindowId = meb.WindowId;
				view.PushInput(Bmotion, local);
				return true;

			/*
			//Touch Screen Drag
			case InputEventScreenDrag drag:
				Vector3 Corigin = GetViewport().GetCamera3D().ProjectRayOrigin(drag.Position);
				Vector3 Cnormal = GetViewport().GetCamera3D().ProjectRayNormal(drag.Position);
				Plane Cpl = new Plane(mesh.GlobalBasis.Z, mesh.GlobalPosition);
				Vector3 Cv3 = Cpl.IntersectsRay(Corigin, Cnormal) ?? Vector3.Zero;
				Cv3 = mesh.GlobalTransform.AffineInverse() * Cv3;
				Vector2 Cv2 = new Vector2(Cv3.X, Cv3.Y);
				Cmotion.ButtonMask = 0;//MouseButtonMask.Left;
				Cmotion.Position = (Cv2 + halfInvSize) / invSize * view.Size;
				Cmotion.GlobalPosition = Cmotion.Position;
				view.PushInput(Cmotion, local);
				return true;

			//Touch Screen Touch
			case InputEventScreenTouch touch:
				Vector3 Dorigin = GetViewport().GetCamera3D().ProjectRayOrigin(touch.Position);
				Vector3 Dnormal = GetViewport().GetCamera3D().ProjectRayNormal(touch.Position);
				Plane Dpl = new Plane(mesh.GlobalBasis.Z, mesh.GlobalPosition);
				Vector3 Dv3 = Dpl.IntersectsRay(Dorigin, Dnormal) ?? Vector3.Zero;
				// worldPos = origin + normal * 10f;
				Dv3 = mesh.GlobalTransform.AffineInverse() * Dv3;
				Vector2 Dv2 = new Vector2(Dv3.X, Dv3.Y);
				// motion.ButtonIndex = touch.Pressed ? MouseButton.Left : MouseButton.None;
				Dmotion.ButtonIndex = MouseButton.Left;
				Dmotion.ButtonMask = touch.Pressed ? MouseButtonMask.Left : 0;
				Dmotion.Pressed = touch.Pressed;
				Dmotion.Position = (Dv2 + halfInvSize) / invSize * view.Size;
				Dmotion.GlobalPosition = Dmotion.Position;
				view.PushInput(Dmotion, local);
				return true;
			*/

			default: return false;
		}
	}

	public override void _UnhandledInput(InputEvent ev)
	{
		if (input)
		{
			if (HandleMouse(ev, viewport, this))
			{
				wasInputValid = true;
				// GetViewport().SetInputAsHandled();
			}
			else if (ev is InputEventMouse)
			{
				wasInputValid = false;
			}
			else if (wasInputValid)
			{
				viewport.PushInput(ev);
				// GetViewport().SetInputAsHandled();
			}
		}
	}

	public Vector2 CalcPosition(Vector3 pos, Vector3 dir)
	{
		var invSize = MeshSize;
		invSize.Y = -1f / invSize.Y;
		var halfInvSize = MeshSize * 0.5f;
		halfInvSize.Y *= -1f;

		Plane pl = new Plane(GlobalBasis.Z, GlobalPosition);
		Vector3? v3 = pl.IntersectsRay(pos, dir);
		if (!v3.HasValue)
			return Vector2.Zero;
		v3 = GlobalTransform.AffineInverse() * v3;
		Vector2 v2 = new Vector2(v3.Value.X, v3.Value.Y);

		return new Vector2((v2.X / MeshSize.X) + 0.5f, 0.5f - (v2.Y / MeshSize.Y)) * viewport.Size;
		// return (v2 / invSize) * view.Size;
	}

	public void PushInput(InputEvent ev)
	{
		viewport.PushInput(ev);
	}

	public void HandleRaycastPress(bool leftPressed, Vector3 pos, Vector3 dir)
	{
		if (!IsVisibleInTree())
			return;
		bool local = false;

		// InputEventMouseButton input = new InputEventMouseButton();
		var input = Bmotion;
		input.ButtonIndex = MouseButton.Left;
		input.ButtonMask = leftPressed ? MouseButtonMask.Left : 0;
		input.Pressed = leftPressed;
		input.Position = CalcPosition(pos, dir);
		input.GlobalPosition = input.Position;
		viewport.PushInput(input, local);
	}

	public void HandleRaycastMotion(bool leftPressed, Vector3 pos, Vector3 dir, Vector3 lastPos, Vector3 lastDir)
	{
		if (!IsVisibleInTree())
			return;
		bool local = false;

		var input = Amotion;
		// InputEventMouseMotion input = new InputEventMouseMotion();
		input.ButtonMask = leftPressed ? MouseButtonMask.Left : 0;
		input.Pressure = leftPressed ? 1f : 0f;
		input.Position = CalcPosition(pos, dir);
		input.Relative = input.Position - CalcPosition(lastPos, lastDir);
		input.GlobalPosition = input.Position;
		viewport.PushInput(input, local);
	}
}
