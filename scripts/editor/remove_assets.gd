@tool
extends EditorScript

func _run() -> void:
	var csv := FileAccess.open("user://file_usages.csv", FileAccess.WRITE)
	var dict := {}
	var sizes := {}
	iterate("res://", csv, dict, sizes)
	for key in dict:
		csv.store_csv_line([key, str(dict[key]), str(sizes[key])])
	csv.close()

func iterate(path: String, csv: FileAccess, dict: Dictionary, sizes: Dictionary) -> void:
	#if path.begins_with("res://."):
		#return
	var dir := DirAccess.open(path)
	if dir.file_exists(".gdignore"):
		return
	for folder in dir.get_directories():
		iterate(path.path_join(folder), csv, dict, sizes)
	for file in dir.get_files():
		if file.get_extension() == "import":
			continue
		var full_file := path.path_join(file)
		var count := ResourceLoader.get_dependencies(full_file)
		var deps := []
		for dep in count:
			var full_dep := dep.get_slice("::", 2)
			deps.append(full_dep)
			if not dict.has(full_dep):
				dict[full_dep] = 0
			dict[full_dep] += 1
		if not dict.has(full_file):
			dict[full_file] = 0
		if not sizes.has(full_file):
			var fs := FileAccess.open(full_file, FileAccess.READ)
			var size := fs.get_length()
			fs.close()
			sizes[full_file] = size
		#if csv:
			#csv.store_csv_line([full_file, str(deps)])
		#else:
			#prints(full_file, str(deps))
