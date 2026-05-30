@tool
extends EditorPlugin

const CFG_FILE := "res://pck_manager.cfg"
const CFG_SECTION := "PCK_splits"
const CFG_FEATURE := "split_pck"

const CFG_TOOL_MENU_NAME := "Export split .pck"

class PCKSplit extends EditorExportPlugin:
	var config: ConfigFile = ConfigFile.new()
	var pck_name: String = ""
	
	func _get_name() -> String:
		return "PCKSplit"
	
	func _export_begin(features: PackedStringArray, is_debug: bool, path: String, flags: int) -> void:
		print("Exporting to path: %s" % path)
		pck_name = path
		config.clear()
		config.load(CFG_FILE)
		var paths := config.get_section_keys(CFG_SECTION)
		for p in paths:
			var value := config.get_value(CFG_SECTION, p) as String
			if path.ends_with(value):
				print("pck name: %s" % path)
				pck_name = path
				break
	
	func _export_end() -> void:
		print("Exported to path: %s" % pck_name)
		var path := pck_name
		var paths := config.get_section_keys(CFG_SECTION)
		if path.ends_with(".pck") or path.is_empty():
			return
		for p in paths:
			var value := config.get_value(CFG_SECTION, p) as String
			var platform := get_export_platform()
			var preset := get_export_preset()
			var pck_path := path.path_join("..").path_join(value).simplify_path()
			if not DirAccess.dir_exists_absolute(pck_path.get_base_dir()):
				DirAccess.make_dir_recursive_absolute(pck_path.get_base_dir())
			platform.export_pack(preset, false, pck_path, 0)
		pck_name = ""
		#if RenderingServer.get_video_adapter_name().is_empty():
			#Engine.get_main_loop().quit()
	
	func _export_file(path: String, type: String, features: PackedStringArray) -> void:
		if not config.has_section(CFG_SECTION):
			return
		var paths := config.get_section_keys(CFG_SECTION)
		for p in paths:
			var value := config.get_value(CFG_SECTION, p) as String
			if path.begins_with(p) and (pck_name.is_empty() or not pck_name.ends_with(value)):
				skip()
				#print("Skipped exporting file: %s" % path)
				break

var plugin: PCKSplit = PCKSplit.new()

static func export_split_pck() -> void:
	pass
	#var pid := OS.create_process(OS.get_executable_path(), ["--headless", "--export-release", "Windows/Steam/Playtest", "--quit"], true)
	#print("pid: %d" % pid)

func _enter_tree() -> void:
	add_export_plugin(plugin)
	add_tool_menu_item(CFG_TOOL_MENU_NAME, export_split_pck)

func _exit_tree() -> void:
	remove_export_plugin(plugin)
	remove_tool_menu_item(CFG_TOOL_MENU_NAME)
	
