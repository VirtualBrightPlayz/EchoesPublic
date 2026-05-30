@tool
class_name ProcWalk
extends SkeletonModifier3D

@export
var bone_name: String
@export
var target: Node3D
@export
var velocity := Vector3.ZERO
@export
var anims: AnimationTree

@export
var height := Vector2(0, 1)

var last_pos := Vector3.ZERO

func _process_modification_with_delta(delta: float) -> void:
	if not target:
		return
	if not anims:
		return
	var skel := get_skeleton()
	var idx := skel.find_bone(bone_name)
	if idx == -1:
		return
	var xform := skel.global_transform * skel.get_bone_global_pose(idx)
	var plane := Plane(skel.global_basis.y, skel.global_position)
	var point := plane.project(xform.origin)
	var d := xform.origin.distance_to(point)
	var ratio := inverse_lerp(height.x, height.y, d)
	target.global_position = point.lerp(xform.origin, ratio)
	#print(d)
	var local_point := skel.to_local(point)
	var speed := (local_point - last_pos).length() / delta
	#anims.set("parameters/TimeScale/scale", speed)
	last_pos = local_point
