@tool
extends EditorScript

func _run() -> void:
	for scene in EditorInterface.get_selection().get_selected_nodes():
		if scene is CollisionObject3D:
			continue
		iterate(scene)
		EditorInterface.mark_scene_as_unsaved()

func iterate(node: Node) -> void:
	if node != null:
		for child in node.get_children():
			iterate(child)
		if node is MeshInstance3D and node.visible and node.mesh is ArrayMesh:
			for i in range(node.mesh.get_surface_count()):
				if node.mesh.surface_get_primitive_type(i) != Mesh.PRIMITIVE_TRIANGLES:
					return
			var col = CollisionShape3D.new()
			col.name = node.name
			col.shape = node.mesh.create_trimesh_shape()
			for n in EditorInterface.get_selection().get_selected_nodes():
				if n is CollisionObject3D:
					n.add_child(col, true)
					col.global_transform = node.global_transform
					break
			col.owner = EditorInterface.get_edited_scene_root()
