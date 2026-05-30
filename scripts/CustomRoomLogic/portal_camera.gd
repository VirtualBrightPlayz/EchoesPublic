@tool
extends Camera3D

@export var ref_point: Node3D

func _notification(what: int) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE and get_viewport() is SubViewport:
		transform = Transform3D.IDENTITY
		fov = 75.0
		near = 0.05
		far = 4000.0
		var vp: SubViewport = get_viewport()
		vp.size = Vector2i(512, 512)

func setup_for_camera(cam: Camera3D) -> void:
	if get_viewport() is SubViewport and ref_point:
		var vp: SubViewport = get_viewport()
		vp.size = cam.get_viewport().get_texture().get_size()
		global_transform = ref_point.global_transform.affine_inverse() * cam.global_transform#.affine_inverse()
		fov = cam.fov
		near = cam.near
		far = cam.far

func _process(_delta: float) -> void:
	# if Engine.is_editor_hint():
	# 	var cam = EditorInterface.get_editor_viewport_3d().get_camera_3d()
	# 	if cam:
	# 		setup_for_camera(cam)
	# 	return
	var other: Camera3D = get_viewport().get_parent().get_viewport().get_camera_3d()
	if other:
		setup_for_camera(other)
