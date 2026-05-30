@tool
class_name AssetTest2
extends EditorScript

func _run() -> void:
	#print(ClassDB.class_get_property_list("LightmapGIData"))
	#return
	#var ext := preload("./gltf_lightmap.gd").new()
	#GLTFDocument.register_gltf_document_extension(ext)
	#var state := GLTFState.new()
	#var doc := GLTFDocument.new()
	#doc.append_from_scene(EditorInterface.get_edited_scene_root(), state)
	#doc.write_to_filesystem(state, "user://file.gltf")
	#GLTFDocument.unregister_gltf_document_extension(ext)
	#return
	var fs := FileAccess.open("res://addons/modding/whitelist.txt", FileAccess.READ)
	var lines := fs.get_as_text().replace("\r", "").split("\n", false)
	fs.close()
	var sys := AssetSystem.new()
	sys.class_save_lookup["GDScript"] = _save_script_path
	sys.class_load_lookup["GDScript"] = _load_script_path
	sys.whitelist.append_array(lines)
	sys.open_write("user://map.zip")
	var scn := ResourceLoader.load(EditorInterface.get_edited_scene_root().scene_file_path) as PackedScene
	scn.resource_path = "res://map.dat"
	sys.write_resource(scn)
	sys.write_scene(EditorInterface.get_edited_scene_root(), "res://map2.dat")
	sys.close()
	
	return
	
	sys.open_read("user://map.zip")
	#var scn2 := sys.read_resource("res://map.dat") as PackedScene
	var new_node := sys.read_scene("res://map2.dat")
	print(sys._load_depends)
	while sys.get_dependency_count() > 0:
		sys.read_next_dependency()
	var scn2 := PackedScene.new()
	scn2.pack(new_node)
	ResourceSaver.save(scn2, "user://mod.tscn")
	sys.close()

func _save_script_path(buffer: StreamPeer, scr: GDScript) -> void:
	buffer.put_utf8_string(scr.get_global_name())

func _load_script_path(buffer: StreamPeer, path: String) -> void:
	buffer.get_utf8_string()
