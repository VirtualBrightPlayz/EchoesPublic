@tool
extends EditorScript

func _run() -> void:
	for node in EditorInterface.get_selection().get_top_selected_nodes():
		var editor_plugin: CyclopsLevelBuilder = node.get_tree().root.get_node(CyclopsLevelBuilder.AUTOLOAD_NAME).builder
		for child in node.find_children("*"):
			if child is CSGBox3D and child.operation == CSGShape3D.OPERATION_UNION:
				var cmd:CommandAddBlock = CommandAddBlock.new()
				cmd.builder = editor_plugin
				
				var bounds: AABB = child.global_transform * AABB(child.size / -2.0, child.size)
				cmd.bounds = bounds
				var scene_root = EditorInterface.get_edited_scene_root()
				cmd.blocks_root_path = scene_root.get_path()
				cmd.block_name = GeneralUtil.find_unique_name(scene_root, child.get_parent().name)
				
				var undo:EditorUndoRedoManager = editor_plugin.get_undo_redo()
				cmd.add_to_undo_manager(undo)
