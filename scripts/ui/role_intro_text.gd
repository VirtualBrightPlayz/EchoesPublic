@tool
class_name RoleIntroText
extends Node

@export var label: RichTextLabel
@export_range(0.0, 1.0, 0.01) var ratio: float = 0.0
@export_range(0.0, 1.0, 0.01) var text_ratio: float = 0.0
@export_multiline var text: String
@export var colors: Gradient

func _notification(what: int) -> void:
	if what == NOTIFICATION_EDITOR_PRE_SAVE:
		label.clear()

func _process(delta: float) -> void:
	if label and colors:
		label.clear()
		var text_len := int(text.length() * text_ratio)
		for i in range(text_len):
			var color = colors.sample(float(i) / text_len * (1.0 - ratio))
			label.push_color(color)
			label.add_text(text[i])
			label.pop()
