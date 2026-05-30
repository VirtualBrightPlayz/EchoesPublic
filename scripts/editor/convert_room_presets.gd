@tool
extends EditorScript

func _run() -> void:
	process_dir(EditorInterface.get_current_directory())

func process_dir(dir: String) -> void:
	print(dir)
	for file_name in DirAccess.get_files_at(dir):
		if file_name.get_extension() == "uid":
			continue
		var file := dir.path_join(file_name)
		var res := ResourceLoader.load(file)
		print(file)
		if res is PresetRoom:
			if res.scenes == null or res.scenes.size() == 0:
				continue
			print(res.scenes[0].resource_path)
			res.sceneFile = ResourceUID.path_to_uid(res.scenes[0].resource_path)
			res.scenes = []
			ResourceSaver.save(res, file)
	#for dir_name in DirAccess.get_directories_at(dir):
		#process_dir(dir.path_join(dir_name))
