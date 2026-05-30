
using Godot;

[GlobalClass]
public partial class SecurityCameraMonitorPhysicsProp3D : MotionSensitivePhysicsProp3D, IInteractable
{
	[Export]
	public FacilityCameraStation Station { get; set; }
	
	public void Use(IItemHolder holder, ItemObject item)
	{
		Station.Use(holder, item);   
	}

	public void UseEnd(IItemHolder holder, ItemObject item)
	{
		Station.UseEnd(holder, item);   
	}

	public bool CanUse(IItemHolder holder, ItemObject item)
	{
		return Station.CanUse(holder, item);
	}

	public Vector3 WorldInteractPosition => Station.WorldInteractPosition;
	public IInteractable.InteractType ActionType => Station.ActionType;
}
