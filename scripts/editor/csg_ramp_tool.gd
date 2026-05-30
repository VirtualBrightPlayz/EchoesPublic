@tool
class_name CSGRampTool
extends CSGBox3D

@export_tool_button("Update") var update_btn = update_ramp


func update_ramp() -> void:
	var csg := get_parent() as CSGBox3D
	var dir := Vector2(csg.size.y, csg.size.z)
	var dist := dir.length()
	var angle := asin(csg.size.y / dist)
	size.x = csg.size.x
	size.y = (dir.x * dir.y) / dist
	size.z = dist * 2.0
	position.y = csg.size.y * 0.5
	#position.z = csg.size.y * 0.5
	#position.z = csg.size.y * (csg.size.y / csg.size.z)
	rotation.x = angle
	if Engine.is_editor_hint():
		EditorInterface.mark_scene_as_unsaved()
