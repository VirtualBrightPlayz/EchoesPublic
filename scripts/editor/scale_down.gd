@tool
extends EditorScript

const SIZE_FACTOR: float = 15.0 / 20.8

func get_xform(node: Node3D) -> Transform3D:
	var xform = Transform3D.IDENTITY
	var parent = node
	while parent:
		xform = parent.transform * xform
		parent = parent.get_parent_node_3d()
	return xform

func _run() -> void:
	for scn in EditorInterface.get_selected_paths():
		print(scn)
		
		var pscn := ResourceLoader.load(scn, "", ResourceLoader.CACHE_MODE_REPLACE_DEEP) as PackedScene
		var node := pscn.instantiate()
		
		#var bounds := AABB(Vector3.ZERO, Vector3.ZERO)
		#for ch in node.find_children("*", "VisualInstance3D", true, false):
			#if ch is VisualInstance3D:
				#var xform = get_xform(ch)
				#bounds = bounds.merge(xform * ch.get_aabb())
		#prints(scn, bounds.size)
		
		for ch in node.get_children():
			if ch is Node3D:
				ch.position *= SIZE_FACTOR
				ch.scale *= SIZE_FACTOR
		
		pscn.pack(node)
		ResourceSaver.save(pscn, pscn.resource_path)
		EditorInterface.reload_scene_from_path(pscn.resource_path)
	#EditorInterface.mark_scene_as_unsaved()
