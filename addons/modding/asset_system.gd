@tool
class_name AssetSystem
extends RefCounted

const VERSION := 1

var whitelist := PackedStringArray()
var class_load_lookup: Dictionary[String, Callable] = {}
var class_save_lookup: Dictionary[String, Callable] = {}

var zip_packer: ZIPPacker
var zip_reader: ZIPReader

var _cache: Dictionary[String, Resource] = {}
var _load_depends: Dictionary[Object, PackedStringArray] = {}

func open_read(path: String) -> Error:
	if zip_reader:
		push_error("ZIPReader already open")
		return ERR_FILE_ALREADY_IN_USE
	if zip_packer:
		push_error("ZIPPacker already open")
		return ERR_FILE_ALREADY_IN_USE
	zip_reader = ZIPReader.new()
	assert(zip_reader.open(path) == OK)
	_cache.clear()
	_load_depends.clear()
	return OK

func open_write(path: String) -> Error:
	if zip_reader:
		push_error("ZIPReader already open")
		return ERR_FILE_ALREADY_IN_USE
	if zip_packer:
		push_error("ZIPPacker already open")
		return ERR_FILE_ALREADY_IN_USE
	zip_packer = ZIPPacker.new()
	assert(zip_packer.open(path) == OK)
	zip_packer.compression_level = ZIPPacker.COMPRESSION_NONE
	_cache.clear()
	_load_depends.clear()
	return OK

func close() -> void:
	if zip_packer:
		zip_packer.close()
		zip_packer = null
	if zip_reader:
		zip_reader.close()
		zip_reader = null
	_cache.clear()
	_load_depends.clear()

func is_class_whitelisted(clss: String) -> bool:
	for item in whitelist:
		if item.ends_with("*") and (clss == item.trim_suffix("*") or ClassDB.is_parent_class(clss, item.trim_suffix("*"))):
			return true
	return whitelist.has(clss)

func get_dependency_count() -> int:
	var count := 0
	for obj in _load_depends:
		count += _load_depends[obj].size()
	return count

func read_next_dependency() -> String:
	for obj in _load_depends:
		var property_path := _load_depends[obj][0]
		var res_path := _load_depends[obj][1]
		_load_depends[obj].remove_at(0)
		_load_depends[obj].remove_at(0)
		var res := read_resource(res_path)
		if property_path.contains("."):
			var split := property_path.split(".")
			var base_path := split[0]
			var index := int(split[1])
			var arr := obj.get_indexed(base_path) as Array
			arr[index] = res
			obj.set_indexed(base_path, arr)
		else:
			obj.set_indexed(property_path, res)
		if _load_depends[obj].size() == 0:
			_load_depends.erase(obj)
		return res_path
	return ""

func read_resource(path: String) -> Resource:
	assert(not path.is_empty())
	#print("Reading resource %s" % path)
	var zip_path := get_zip_path(path)
	if _cache.has(zip_path):
		return _cache.get(zip_path)
	var buffer := StreamPeerBuffer.new()
	buffer.data_array = zip_reader.read_file(zip_path)
	var version := buffer.get_u32()
	assert(version == VERSION, "Version mismatch, %s" % version)
	var clss := buffer.get_utf8_string()
	if class_load_lookup.has(clss):
		var callback := class_load_lookup.get(clss) as Callable
		var data := callback.call(buffer, path) as Resource
		_cache.set(zip_path, data)
		return data
	if clss == "PackedScene":
		var scn := read_scene_raw(buffer, path)
		_cache.set(zip_path, scn)
		return scn
	if clss == "Script" or ClassDB.is_parent_class(clss, "Script"):
		#print("Script: %s" % path)
		return null
	if clss == "Texture2D" or ClassDB.is_parent_class(clss, "Texture2D"):
		var tex := read_texture2d(buffer, path)
		_cache.set(zip_path, tex)
		return tex
	if clss == "TextureLayered" or ClassDB.is_parent_class(clss, "TextureLayered"):
		var tex := read_texture2d_array(buffer, path)
		_cache.set(zip_path, tex)
		return tex
	if clss == "AudioStreamOggVorbis":
		var ogg := read_ogg(buffer, path)
		_cache.set(zip_path, ogg)
		return ogg
	var res := create_class_safe(clss)
	if res != null:
		get_properties(buffer, res, false)
		_cache.set(zip_path, res)
		return res
	return null

func write_resource(res: Resource) -> void:
	assert(res != null)
	print("Writing resource %s" % res.resource_path)
	var zip_path := get_zip_path(res.resource_path)
	if _cache.has(zip_path):
		#print("%s is already cached." % zip_path)
		return
	_cache.set(zip_path, res)
	var buffer := StreamPeerBuffer.new()
	buffer.put_u32(VERSION) # version header
	buffer.put_utf8_string(res.get_class())
	if class_save_lookup.has(res.get_class()):
		var callback := class_save_lookup.get(res.get_class()) as Callable
		callback.call(buffer, res)
	elif res is Script:
		pass
	elif res is PackedScene:
		write_scene_raw(buffer, res)
	elif res is Texture2D:
		write_texture2d(buffer, res)
	elif res is TextureLayered:
		write_texture2d_array(buffer, res)
	elif res is AudioStreamOggVorbis:
		write_ogg(buffer, res)
	else:
		put_properties(buffer, res)
	# write data to zip
	zip_packer.start_file(zip_path)
	zip_packer.write_file(buffer.data_array)
	zip_packer.close_file()

func read_scene(path: String) -> Node:
	assert(not path.is_empty())
	print("Reading scene %s" % path)
	var zip_path := get_zip_path(path)
	var buffer := StreamPeerBuffer.new()
	buffer.data_array = zip_reader.read_file(zip_path)
	var version := buffer.get_u32()
	assert(version == VERSION, "Version mismatch, %s" % version)
	var root := get_node(buffer, true)
	if root != null:
		for child in root.find_children("*", "", true, false):
			child.owner = root
		return root
	return null

func write_scene(root: Node, path: String) -> void:
	assert(root != null)
	assert(not path.is_empty())
	print("Writing scene %s" % path)
	var zip_path := get_zip_path(path)
	var buffer := StreamPeerBuffer.new()
	buffer.put_u32(VERSION) # version header
	put_node(buffer, root) # root node (and children)
	# write data to zip
	zip_packer.start_file(zip_path)
	zip_packer.write_file(buffer.data_array)
	zip_packer.close_file()

func read_scene_raw(buffer: StreamPeerBuffer, path: String) -> PackedScene:
	assert(not path.is_empty())
	#print("Reading scene %s" % path)
	var version := buffer.get_u32()
	assert(version == VERSION, "Version mismatch, %s" % version)
	var root := get_node(buffer, false)
	while get_dependency_count() > 0:
		read_next_dependency()
	if root != null:
		for child in root.find_children("*", "", true, false):
			child.owner = root
		var scene := PackedScene.new()
		var err := scene.pack(root)
		assert(err == OK, error_string(err))
		root.queue_free()
		return scene
	return null

func write_scene_raw(buffer: StreamPeerBuffer, scene: PackedScene) -> void:
	assert(scene != null)
	print("Writing scene (raw) %s" % scene.resource_path)
	var root := scene.instantiate()
	buffer.put_u32(VERSION) # version header
	put_node(buffer, root) # root node (and children)
	root.queue_free()

func read_ogg(buffer: StreamPeer, path: String) -> AudioStreamOggVorbis:
	assert(not path.is_empty())
	#print("Reading ogg %s" % path)
	var loop := buffer.get_u8() == 1
	var size := buffer.get_u64()
	var data := buffer.get_data(size)[1] as PackedByteArray
	var ogg := AudioStreamOggVorbis.load_from_buffer(data)
	ogg.loop = loop
	return ogg

func write_ogg(buffer: StreamPeer, ogg: AudioStreamOggVorbis) -> void:
	assert(ogg != null and FileAccess.file_exists(ogg.resource_path))
	print("Writing ogg %s" % ogg.resource_path)
	var data := FileAccess.get_file_as_bytes(ogg.resource_path)
	buffer.put_u8(1 if ogg.loop else 0)
	buffer.put_u64(len(data))
	buffer.put_data(data)

func read_texture2d(buffer: StreamPeer, path: String) -> ImageTexture:
	assert(not path.is_empty())
	#print("Reading texture2d %s" % path)
	var has_img := buffer.get_u8()
	if has_img == 0:
		return null
	var data_len := buffer.get_u64()
	var data := buffer.get_data(data_len)[1] as PackedByteArray
	var format := buffer.get_u16() as Image.Format
	var width := buffer.get_u16()
	var height := buffer.get_u16()
	var mipmaps := buffer.get_u8() == 1
	var img := Image.create_from_data(width, height, mipmaps, format, data)
	var tex := ImageTexture.create_from_image(img)
	return tex

func write_texture2d(buffer: StreamPeer, tex: Texture2D) -> void:
	print("Writing texture2d %s" % tex.resource_path)
	var img := tex.get_image()
	if img:
		buffer.put_u8(1)
		buffer.put_u64(img.get_data_size())
		buffer.put_data(img.get_data())
		buffer.put_u16(img.get_format())
		buffer.put_u16(img.get_width())
		buffer.put_u16(img.get_height())
		buffer.put_u8(1 if img.has_mipmaps() else 0)
		#buffer.put_var(img.data, false)
	else:
		buffer.put_u8(0)
		#buffer.put_var(null, false)

func read_texture2d_array(buffer: StreamPeer, path: String) -> Texture2DArray:
	var type := buffer.get_16() as TextureLayered.LayeredType
	assert(type == TextureLayered.LAYERED_TYPE_2D_ARRAY)
	var ctex := Texture2DArray.new()
	var siz := buffer.get_u16()
	var imgs: Array[Image] = []
	for i in range(siz):
		var has_img := buffer.get_u8()
		if has_img == 0:
			imgs.append(null)
			continue
		var data_len := buffer.get_u64()
		var data := buffer.get_data(data_len)[1] as PackedByteArray
		var format := buffer.get_u16() as Image.Format
		var width := buffer.get_u16()
		var height := buffer.get_u16()
		var mipmaps := buffer.get_u8() == 1
		var img := Image.create_from_data(width, height, mipmaps, format, data)
		imgs.append(img)
	ctex.create_from_images(imgs)
	return ctex

func write_texture2d_array(buffer: StreamPeer, tex: TextureLayered) -> void:
	buffer.put_16(tex.get_layered_type() as int)
	buffer.put_u16(tex.get_layers())
	for i in range(tex.get_layers()):
		var img := tex.get_layer_data(i)
		if not img:
			buffer.put_u8(0)
			continue
		buffer.put_u8(1)
		buffer.put_u64(img.get_data_size())
		buffer.put_data(img.get_data())
		buffer.put_u16(img.get_format())
		buffer.put_u16(img.get_width())
		buffer.put_u16(img.get_height())
		buffer.put_u8(1 if img.has_mipmaps() else 0)

func create_class_safe(name: String) -> Object:
	if name == "Script" or ClassDB.is_parent_class(name, "Script") or name == "PackedScene":
		return null
	if is_class_whitelisted(name):
		return ClassDB.instantiate(name)
	return null

func get_valid_properties(obj: Object) -> PackedStringArray:
	var prop_names := obj.get_property_list()
	var props := PackedStringArray()
	for prop in prop_names:
		var name := prop["name"] as String
		var usage := prop["usage"] as PropertyUsageFlags
		var type := prop["type"] as Variant.Type
		var clss := prop["class_name"] as StringName
		var valid_types := clss.split(",")
		if usage & PropertyUsageFlags.PROPERTY_USAGE_STORAGE != 0:
			if type == TYPE_OBJECT:
				var allow := false
				for t in valid_types:
					if is_class_whitelisted(t):
						allow = true
						break
				if allow:
					props.append(name)
			else:
				props.append(name)
	if obj is MeshInstance3D: # HACK
		if obj.mesh:
			for i in range(obj.mesh.get_surface_count()):
				props.append("surface_material_override/%d" % i)
	if obj is ShaderMaterial: # HACK
		if obj.shader:
			for arg in obj.shader.get_shader_uniform_list():
				props.append("shader_parameter/%s" % arg["name"])
	return props

func get_node(buffer: StreamPeer, cache_only: bool) -> Node:
	var name := buffer.get_utf8_string()
	var path := buffer.get_utf8_string()
	var clss := buffer.get_utf8_string()
	var node := create_class_safe(clss) as Node
	if node == null:
		return null
	node.name = name
	get_properties(buffer, node, cache_only)
	var count := buffer.get_u64()
	for i in range(count):
		var child := get_node(buffer, cache_only)
		if child == null:
			continue
		#assert(child != null, "Error reading child node #%s" % i)
		node.add_child(child, true)
	return node

func put_node(buffer: StreamPeer, node: Node) -> void:
	buffer.put_utf8_string(node.name) # name
	buffer.put_utf8_string(get_node_path(node)) # path
	buffer.put_utf8_string(node.get_class())
	if not is_class_whitelisted(node.get_class()):
		return
	put_properties(buffer, node)
	buffer.put_u64(node.get_child_count()) # child count
	for i in range(node.get_child_count()):
		put_node(buffer, node.get_child(i)) # node

func get_properties(buffer: StreamPeer, obj: Object, cache_only: bool) -> void:
	var props := get_valid_properties(obj)
	var size := buffer.get_u64()
	for i in range(size):
		var prop_name := buffer.get_utf8_string()
		var force_allow := false
		if obj is MeshInstance3D: # HACK
			if prop_name.begins_with("surface_material_override/"):
				force_allow = true
		if obj is ShaderMaterial: # HACK
			if prop_name.begins_with("shader_parameter/"):
				force_allow = true
		if not props.has(prop_name) and not force_allow:
			get_value(buffer, null, "") # just to skip forward in the buffer
			continue
		var value: Variant = null
		if cache_only:
			value = get_value(buffer, obj, prop_name)
		else:
			value = get_value(buffer, null, "")
		obj.set(prop_name, value)

func put_properties(buffer: StreamPeer, obj: Object) -> void:
	var props := get_valid_properties(obj)
	buffer.put_u64(props.size())
	for i in range(props.size()):
		buffer.put_utf8_string(props[i])
		var value := obj.get(props[i])
		put_value(buffer, value)

func get_value(buffer: StreamPeer, obj: Object, property_path: String) -> Variant:
	var type := buffer.get_u8()
	match type:
		TYPE_NIL:
			return null
		TYPE_OBJECT:
			var is_file := buffer.get_u8()
			if is_file != 0 or true:
				var path := buffer.get_utf8_string()
				if path.is_empty(): # HACK
					return null
				var zip_path := get_zip_path(path)
				if _cache.has(zip_path):
					return _cache[zip_path]
				if property_path.is_empty() or obj == null:
					return read_resource(path)
				if not _load_depends.has(obj):
					_load_depends[obj] = []
				_load_depends[obj].append(property_path)
				_load_depends[obj].append(path)
				return null
				#return read_resource(path)
			else:
				#var clss := buffer.get_utf8_string()
				#var value := create_class_safe(clss)
				#get_properties(buffer, value, false)
				#return value
				return null
		TYPE_ARRAY:
			var size := buffer.get_u64()
			var arr := Array()
			arr.resize(size)
			for i in range(size):
				arr[i] = get_value(buffer, obj, property_path + "." + str(i))
			return arr
		TYPE_DICTIONARY:
			var size := buffer.get_u64()
			var dict := Dictionary()
			for i in range(size):
				var key := get_value(buffer, null, "")
				var val := get_value(buffer, obj, property_path + ":" + str(key))
				dict.set(key, val)
			return dict
		_:
			return buffer.get_var()

func put_value(buffer: StreamPeer, value: Variant) -> void:
	var type: Variant.Type = typeof(value)
	if type == TYPE_NIL:
		buffer.put_u8(TYPE_NIL)
	elif type == TYPE_OBJECT:
		if value is Resource and is_class_whitelisted(value.get_class()):
			buffer.put_u8(type)
			if true:# and value is Script or \
				#value is PackedScene or \
				#value is Texture2D or \
				#not value.is_built_in():
				buffer.put_u8(1) # yes, we're a file
				var str := value.resource_path as String
				buffer.put_utf8_string(str)
				write_resource(value)
			else:
				buffer.put_u8(0) # no, we're part of the scene
				buffer.put_utf8_string(value.get_class())
				put_properties(buffer, value)
		else:
			buffer.put_u8(TYPE_NIL)
			return # don't write any objects outside of resources
	elif type == TYPE_ARRAY:
		buffer.put_u8(type)
		buffer.put_u64(len(value))
		for i in range(len(value)):
			put_value(buffer, value[i])
	elif type == TYPE_DICTIONARY:
		buffer.put_u8(type)
		var keys := value.keys() as Array
		buffer.put_u64(len(keys))
		for i in range(len(keys)):
			var key := keys[i] as Variant
			put_value(buffer, key)
			put_value(buffer, value[key])
	else:
		buffer.put_u8(type)
		buffer.put_var(value)

static func get_node_path(node: Node) -> String:
	var path := PackedStringArray()
	var parent := node
	#path.append(parent.name)
	while parent != null:
		path.append(parent.name)
		parent = parent.get_parent()
	path.reverse()
	return "/".join(path)

static func get_zip_path(path: String) -> String:
	return path.trim_prefix("res://")
