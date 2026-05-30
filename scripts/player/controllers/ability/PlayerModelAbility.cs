using System;
using System.Linq;
using Godot;

[Obsolete]
[GlobalClass]
public partial class PlayerModelAbility : BaseAbility
{
    [Export] public PlayerModel model;
    [Export] public Node3D[] disableLocal = [];
    [Export(PropertyHint.Layers3DRender)] public uint localCullFlags = uint.MinValue;

    public override void SetupFromDefinition()
    {
    }

    public override void _Ready()
    {
        base._Ready();
        if (Player.IsLocalPlayer)
        {
            foreach (var item in disableLocal)
            {
                foreach (var inst in item.FindChildren("*", nameof(VisualInstance3D), owned: false))
                {
                    if (inst is VisualInstance3D vis)
                    {
                        vis.Layers = localCullFlags;
                    }
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        UpdateModel(delta);
        if (Player.IsLocalPlayer)
        {
            HandleAnimations(delta);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        UpdateModelPhysics(delta);
        if (Player.IsLocalPlayer && Player.ActiveController is FPController fp && IsInstanceValid(model) && IsInstanceValid(model.anims))
        {
            float targetWalk;
            if (fp.MovementDirection.IsZeroApprox())
            {
                targetWalk = 0f;
            }
            else
            {
                targetWalk = fp.Velocity.Length() / fp.EffectiveSpeed;
            }
            model.anims.walkingDirection = fp.MovementDirection;
            model.anims.walking = targetWalk;
        }
    }

    public virtual void HandleAnimations(double delta)
    {
        if (IsInstanceValid(model) && IsInstanceValid(model.anims) && Player.TryGetAbility(out InventoryAbility inventory))
        {
            if (inventory.InventoryEquipped.Any(x => x.model is Pistol))
            {
                model.anims.heldItem = inventory.InventoryEquipped.Where(x => x.model is Pistol).Select(x => (Pistol)x.model).FirstOrDefault().holdType;
            }
            else if (inventory.InventoryEquipped.Any(/*x => x.model is Flashlight*/))
            {
                model.anims.heldItem = PlayerAnims.HeldItemType.SmallItem;
            }
            else
            {
                model.anims.heldItem = PlayerAnims.HeldItemType.None;
            }
        }
    }

    public virtual void UpdateModel(double delta)
    {
        if (IsInstanceValid(model))
        {
            model.headAngle = Player.ActiveController.View.Rotation.X;
            if (Player.IsVR)
            {
                for (int i = 0; i < Player.HandSyncTargets.Length; i++)
                {
                    model.SetPositionRotationForIk((PlayerModel.BoneName)i, delta, Player.HandSyncTargets[i].GlobalPosition, Player.HandSyncTargets[i].GlobalRotation);
                }
            }
#if false
            else
            {
                Transform3D left = model.GetGlobalPose(PlayerModel.BoneName.Left);
                Transform3D right = model.GetGlobalPose(PlayerModel.BoneName.Right);
                // Transform3D head = model.GetGlobalPose(PlayerModel.BoneName.Head);
                model.SetPositionRotationForIk(PlayerModel.BoneName.Left, delta, left.Origin, left.Basis.GetEuler(), false);
                model.SetPositionRotationForIk(PlayerModel.BoneName.Right, delta, right.Origin, right.Basis.GetEuler(), false);
                model.SetPositionRotationForIk(PlayerModel.BoneName.Head, delta, GlobalPosition + (View.GlobalPosition - GlobalPosition), View.GlobalRotation);
            }
#endif
        }
    }

    public virtual void UpdateModelPhysics(double delta)
    {
        // the 41 is layers 1, 4, and 6 as a mask.
        if (IsInstanceValid(model) && Player.IsVR)
        {
            {
                Transform3D hipsRest = model.GetGlobalRest(PlayerModel.BoneName.Hips);
                Transform3D hips = model.GetGlobalPose(PlayerModel.BoneName.Hips);
                Transform3D footRest = model.GetGlobalRest(PlayerModel.BoneName.LeftFoot);
                Transform3D footPos = model.GetGlobalPose(PlayerModel.BoneName.LeftFoot);
                var args = PhysicsRayQueryParameters3D.Create(hips.Origin, footPos.Origin, 41);
                var results = Player.GetWorld3D().DirectSpaceState.IntersectRay(args);
                Transform3D pos = footPos;
                if (results.ContainsKey("position"))
                {
                    Vector3 offset = (hipsRest.Origin - model.hipsOffset) - footRest.Origin;
                    pos.Origin = results["position"].AsVector3() - offset;
                    // pos.Basis = Basis.LookingAt(Vector3.Forward, results["normal"].AsVector3());
                }
                model.SetPositionRotationForIk(PlayerModel.BoneName.LeftFoot, delta, pos.Origin, pos.Basis.GetEuler());
            }
            {
                Transform3D hipsRest = model.GetGlobalRest(PlayerModel.BoneName.Hips);
                Transform3D hips = model.GetGlobalPose(PlayerModel.BoneName.Hips);
                Transform3D footRest = model.GetGlobalRest(PlayerModel.BoneName.RightFoot);
                Transform3D footPos = model.GetGlobalPose(PlayerModel.BoneName.RightFoot);
                var args = PhysicsRayQueryParameters3D.Create(hips.Origin, footPos.Origin, 41);
                var results = Player.GetWorld3D().DirectSpaceState.IntersectRay(args);
                Transform3D pos = footPos;
                if (results.ContainsKey("position"))
                {
                    Vector3 offset = (hipsRest.Origin - model.hipsOffset) - footRest.Origin;
                    pos.Origin = results["position"].AsVector3() - offset;
                    // pos.Basis = Basis.LookingAt(Vector3.Forward, results["normal"].AsVector3());
                }
                model.SetPositionRotationForIk(PlayerModel.BoneName.RightFoot, delta, pos.Origin, pos.Basis.GetEuler());
            }
        }
    }
}