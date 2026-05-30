@tool
class_name TextLines
extends Node

@export var label: RichTextLabel
@export_multiline var text: String:
	set(v):
		text = v
		colors.resize(v.split("\n").size())
		notify_property_list_changed()
@export var lines_visible: int = 0
@export var inverse: bool = false
var colors: PackedColorArray = PackedColorArray()

func _get_property_list() -> Array[Dictionary]:
	var props: Array[Dictionary] = []
	for i in range(colors.size()):
		props.append({
			"name": "color_%d" % i,
			"type": TYPE_COLOR,
		})
	return props

func _get(property: StringName) -> Variant:
	if property.begins_with("color_"):
		var index = property.get_slice("_", 1).to_int()
		if index < colors.size():
			return colors[index]
	return null

func _set(property: StringName, value: Variant) -> bool:
	if property.begins_with("color_"):
		var index = property.get_slice("_", 1).to_int()
		if index < colors.size():
			colors[index] = value
			return true
	return false

func _notification(what: int) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		label.clear()

func _process(delta: float) -> void:
	if label:
		label.clear()
		var lines := text.split("\n")
		var arr := range(lines.size())
		if lines.size() == colors.size():
			for i in arr:
				label.push_color(colors[i])
				if lines_visible == -1:
					label.append_text(lines[i] + "\n")
				elif inverse and i >= lines.size() - lines_visible:
					label.append_text(lines[i] + "\n")
				elif not inverse and i < lines_visible:
					label.append_text(lines[i] + "\n")
				else:
					label.append_text("\n")
				label.pop()
