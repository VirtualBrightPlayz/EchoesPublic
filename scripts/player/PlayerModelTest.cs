using System;
using Godot;

public partial class PlayerModelTest : Marker3D
{
    [Export]
    public PlayerModel model;
    [Export]
    public Node3D head;
    [Export]
    public Node3D leftHand;
    [Export]
    public Node3D rightHand;
    [Export]
    public RayCast3D raycast;

    private Transform3D leftFootPose;
    private Transform3D rightFootPose;

    public override void _Ready()
    {
        // model.skeleton.PoseUpdated += UpdatePose;
    }

    private void UpdatePose()
    {
        leftFootPose = model.GetGlobalPose(PlayerModel.BoneName.LeftFoot);
        rightFootPose = model.GetGlobalPose(PlayerModel.BoneName.RightFoot);
    }

    public override void _PhysicsProcess(double delta)
    {
        {
            Transform3D hipsRest = model.GetGlobalRest(PlayerModel.BoneName.Hips);
            Transform3D hips = model.GetGlobalPose(PlayerModel.BoneName.Hips);
            Transform3D footRest = model.GetGlobalRest(PlayerModel.BoneName.LeftFoot);
            Transform3D footPos = model.GetGlobalPose(PlayerModel.BoneName.LeftFoot);
            var args = PhysicsRayQueryParameters3D.Create(hips.Origin, footPos.Origin, raycast.CollisionMask);
            var results = GetWorld3D().DirectSpaceState.IntersectRay(args);
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
            var args = PhysicsRayQueryParameters3D.Create(hips.Origin, footPos.Origin, raycast.CollisionMask);
            var results = GetWorld3D().DirectSpaceState.IntersectRay(args);
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

    public override void _Process(double delta)
    {
        model.SetPositionRotationForIk(PlayerModel.BoneName.Left, delta, leftHand.GlobalPosition, leftHand.GlobalRotation);
        model.SetPositionRotationForIk(PlayerModel.BoneName.Right, delta, rightHand.GlobalPosition, rightHand.GlobalRotation);
        model.SetPositionRotationForIk(PlayerModel.BoneName.Head, delta, head.GlobalPosition, head.GlobalRotation);
    }
}