@tool
extends EditorScript

func _run() -> void:
	for scn in EditorInterface.get_selected_paths():
		#print(scn)
		#continue
		var found := false
		var pscn := load(scn) as PackedScene
		var node := pscn.instantiate()
		for child in node.get_children():
			if node is RigidbodySync and child is TransformSync:
				found = true
				node.xformSync = child
				if (child.targetNode == null or child.targetNode.is_empty()):
					child.targetNode = child.get_path_to(node)
		if found:
			print(scn)
			#continue
			pscn.pack(node)
			ResourceSaver.save(pscn, pscn.resource_path)
			EditorInterface.reload_scene_from_path(pscn.resource_path)
		node.queue_free()
	#EditorInterface.mark_scene_as_unsaved()
