@tool
class_name AssetTest
extends Node

@export var scene: PackedScene

@export var allowed_types: Array[String] = []
@export var type_lookup: Dictionary[String, String] = {}
@export var prefab_lookup: Dictionary[String, PackedScene] = {}

@export_tool_button("Export")
var export = run_export

@export_tool_button("Import")
var import = run_import

func run_export() -> void:
	print(export_scene(scene))
	return
	
	var zip = ZIPPacker.new()
	zip.open("res://output.zip")
	zip.start_file("scene.dat")
	zip.write_file(export_scene(scene))
	zip.close_file()
	zip.close()
	return
	
	var sys = AssetSystem.new()
	sys.open("res://output.zip")
	sys.add_texture("res://icon_new.png")
	sys.close()

func run_import() -> void:
	var scn = import_scene(export_scene(scene)).instantiate()
	print(scn)
	add_child(scn, true)
	for ch in scn.find_children("*", "", true, false):
		ch.owner = owner
	scn.owner = owner
	return
	
	var zip = ZIPReader.new()
	zip.open("res://output.zip")
	if zip.file_exists("modmeta.txt"):
		var mod_meta = JSON.parse_string(zip.read_file("modmeta.txt").get_string_from_utf8())
		var fmt_ver = mod_meta["format_version"]
	zip.close()

func export_scene(scene: PackedScene) -> PackedByteArray:
	var buffer = StreamPeerBuffer.new()
	var state = scene.get_state()
	var ref_lookup: Array[Object] = []
	buffer.put_u32(1) # version header
	# write the scene
	buffer.put_u32(state.get_node_count())
	for i in range(state.get_node_count()):
		var name = state.get_node_name(i)
		var path = state.get_node_path(i, true)
		var owner_path = state.get_node_owner_path(i)
		var type = state.get_node_type(i)
		buffer.put_utf8_string(name)
		buffer.put_utf8_string(path)
		buffer.put_utf8_string(owner_path)
		buffer.put_utf8_string(type)
		buffer.put_u32(state.get_node_property_count(i))
		for j in range(state.get_node_property_count(i)):
			buffer.put_utf8_string(state.get_node_property_name(i, j))
			var val = state.get_node_property_value(i, j)
			put_ref_value(buffer, val, ref_lookup)
		print("export: path=%s name=%s owner_path=%s type=%s" % [path, name, owner_path, type])
	# collect the rest of the resources
	var ref_queue = []
	while ref_queue.size() > 0:
		var ref_val = ref_queue.pop_back()
		var prop_list = ref_val.get_property_list()
		for prop in prop_list:
			var name = prop["name"] as String
			var usage = prop["usage"] as PropertyUsageFlags
			if usage & PROPERTY_USAGE_STORAGE != 0:
				var val = ref_val.get(name)
				if typeof(val) == TYPE_OBJECT and not ref_lookup.has(val):
					ref_queue.push_back(val)
					ref_lookup.push_back(val)
	# write the ref_lookup
	buffer.put_u32(ref_lookup.size())
	for i in range(ref_lookup.size()):
		var ref_val = ref_lookup[i]
		var prop_list = ref_val.get_property_list()
		var props = {}
		for prop in prop_list:
			var name = prop["name"] as String
			var usage = prop["usage"] as PropertyUsageFlags
			if usage & PROPERTY_USAGE_STORAGE != 0:
				var val = ref_val.get(name)
				props[name] = val
		buffer.put_utf8_string(ref_val.get_class())
		buffer.put_u32(props.size())
		for prop_name in props:
			buffer.put_utf8_string(prop_name)
			if typeof(props[prop_name]) == TYPE_OBJECT:
				var idx = ref_lookup.find(props[prop_name])
				buffer.put_u8(1)
				buffer.put_u32(idx)
			else:
				buffer.put_u8(0)
				buffer.put_var(props[prop_name])
			print("export: i=%s name=%s val=%s" % [i, prop_name, props[prop_name]])
	return buffer.data_array

func import_scene(bytes: PackedByteArray) -> PackedScene:
	var buffer = StreamPeerBuffer.new()
	buffer.data_array = bytes
	var scene_root: Node
	buffer.get_u32() # version header
	var node_count = buffer.get_u32()
	for i in range(node_count):
		var name = StringName(buffer.get_utf8_string())
		var path = NodePath(buffer.get_utf8_string())
		var owner_path = NodePath(buffer.get_utf8_string())
		var type = StringName(buffer.get_utf8_string())
		var prop_count = buffer.get_u32()
		var props = {}
		for j in range(prop_count):
			var prop_name = StringName(buffer.get_utf8_string())
			var prop_value = get_ref_value(buffer)
			props[prop_name] = prop_value
		print("import: path=%s name=%s owner_path=%s props=%s" % [path, name, owner_path, props])
		if path.is_empty():
			scene_root = node_from_type(type)
			scene_root.name = name
		elif not path.is_empty() and scene_root:
			var node = node_from_type(type)
			scene_root.get_node(path).add_child(node)
			node.name = name
			if owner_path.is_empty():
				node.owner = scene_root
			else:
				node.owner = scene_root.get_node(owner_path)
		else:
			printerr("import: name=%s path=%s" % [name, path])
	var ref_count = buffer.get_u32()
	for i in range(ref_count):
		var type = StringName(buffer.get_utf8_string())
		var prop_count = buffer.get_u32()
		var props = {}
		for j in range(prop_count):
			var prop_name = StringName(buffer.get_utf8_string())
			var prop_value = get_ref_value(buffer)
			props[prop_name] = prop_value
	var scene = PackedScene.new()
	scene.pack(scene_root)
	scene_root.queue_free()
	return scene

func node_from_type(type: String) -> Node:
	if allowed_types.has(type):
		return ClassDB.instantiate(type)
	printerr("import: type=%s" % [type])
	return Node3D.new()

func put_ref_value(buffer: StreamPeer, value: Variant, lookup: Array) -> void:
	if typeof(value) == TYPE_OBJECT:
		buffer.put_u8(1)
		buffer.put_u32(lookup.size())
		lookup.push_back(value)
	else:
		buffer.put_u8(0)
		buffer.put_var(value)

func get_ref_value(buffer: StreamPeer) -> Variant:
	if buffer.get_u8() == 0:
		return buffer.get_var()
	return buffer.get_u32()
