@tool
extends EditorExportPlugin

func _export_begin(features, is_debug, path, flags):
	if path.to_lower().ends_with(".pck"):
		return
	var dirPath := ProjectSettings.globalize_path("res://").path_join("export_data/all/")
	copy_files(path.get_base_dir(), dirPath)
	if features.has("windows"):
		dirPath = ProjectSettings.globalize_path("res://").path_join("export_data/windows/")
	elif features.has("linux"):
		dirPath = ProjectSettings.globalize_path("res://").path_join("export_data/linux/")
	else:
		return
	copy_files(path.get_base_dir(), dirPath)

func copy_files(path: String, dirPath: String) -> void:
	var dir := DirAccess.open(dirPath)
	for folder in dir.get_directories():
		prints("Creating folder", folder)
		DirAccess.make_dir_recursive_absolute(path.path_join(folder))
		copy_files(path.path_join(folder), dirPath.path_join(folder))
	for file in dir.get_files():
		prints("Copying file", file, "to", path.path_join(file))
		DirAccess.copy_absolute(dirPath.path_join(file), path.path_join(file))
