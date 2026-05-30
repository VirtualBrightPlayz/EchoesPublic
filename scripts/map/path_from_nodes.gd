@tool
class_name PathFromNodes
extends Path3D

func _enter_tree() -> void:
	build()

func build() -> void:
	curve.clear_points()
	for ch in get_children():
		if ch.visible and ch is Marker3D:
			curve.add_point(ch.position, ch.basis.z * ch.gizmo_extents, -ch.basis.z * ch.gizmo_extents)
			curve.set_point_tilt(curve.point_count - 1, -ch.rotation.z)

func _notification(what: int) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		build()
