@tool
extends EditorExportPlugin

func _export_begin(features, is_debug, path, flags):
	if path.to_lower().ends_with(".pck"):
		return
	if features.has("windows"):
		var dir = path.get_base_dir().path_join("opus.dll")
		DirAccess.copy_absolute("res://opus.dll", dir)
	elif features.has("linux"):
		var dir = path.get_base_dir().path_join("libopus.so")
		DirAccess.copy_absolute("res://libopus.so", dir)
