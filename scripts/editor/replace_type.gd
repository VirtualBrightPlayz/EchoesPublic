@tool
extends EditorScript

const CAN_CLAIM := true

func _run() -> void:
	for scn in EditorInterface.get_selected_paths():
		#continue
		var found := false
		var pscn := load(scn) as PackedScene
		var node := pscn.instantiate()
		for sync in node.find_children("*"):
			if sync is TransformSyncNative:
				replace2(sync)
				found = true
			#else:
				#sync.set("CanClaim", CAN_CLAIM)
		if found:
			print(scn)
			pscn.pack(node)
			ResourceSaver.save(pscn, pscn.resource_path)
			EditorInterface.reload_scene_from_path(pscn.resource_path)
		node.queue_free()
	#EditorInterface.mark_scene_as_unsaved()

func replace2(sync: TransformSyncNative) -> void:
	var sync_sharp := TransformSync.new()
	sync_sharp.name = sync.name
	var o := sync.owner
	sync.replace_by(sync_sharp)
	sync_sharp.targetNode = sync.target_node
	sync_sharp.get_parent().set("sync", null)
	sync_sharp.get_parent().set("xformSync", sync_sharp)
	sync_sharp.set("CanClaim", CAN_CLAIM)
	sync_sharp.owner = o

func replace(sync: Node) -> void:
	var sync_native := TransformSyncNative.new()
	sync_native.name = sync.name
	var o := sync.owner
	sync.replace_by(sync_native)
	sync_native.target_node = sync.target_node
	sync_native.get_parent().set("sync", sync_native)
	sync_native.owner = o
