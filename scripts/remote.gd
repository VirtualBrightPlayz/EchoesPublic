@tool
extends RemoteTransform3D

@export var root: Node3D

@export_tool_button("Run")
var run_btn = run

func run() -> void:
	var parent: Node3D = get_parent()
	var rest: Transform3D
	var skel: Skeleton3D
	if parent is BoneAttachment3D:
		rest = parent.get_skeleton().get_bone_global_rest(parent.bone_idx)
		skel = parent.get_skeleton()
	elif parent is ModifierBoneTarget3D:
		rest = parent.get_skeleton().get_bone_rest(parent.bone)
		skel = parent.get_skeleton()
	else:
		return
	position = root.position
	#transform = root.transform
	#transform = parent.transform.inverse() * skel.global_transform * rest
	#var other: Node3D = get_node(remote_path)
	#global_basis = other.basis.inverse()
	#quaternion = skel.quaternion.inverse()

func _process(delta: float) -> void:
	pass
	#run()
