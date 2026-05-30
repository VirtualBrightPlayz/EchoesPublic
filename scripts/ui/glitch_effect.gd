@tool
class_name GlitchEffect
extends RichTextEffect


# To use this effect:
# - Enable BBCode on a RichTextLabel.
# - Register this effect on the label.
# - Use [glitch param=2.0]hello[/glitch] in text.
var bbcode = "glitch"


func _process_custom_fx(char_fx):
	if floori(char_fx.elapsed_time) % 6 != 0:
		return true
	if ceili(char_fx.elapsed_time) - char_fx.elapsed_time > 0.1:
		return true
	if randf() > 0.2:
		return true

	var text = TextServerManager.get_primary_interface()
	var buf = "#!?@$%".to_ascii_buffer()
	var ch = buf[randi_range(0, len(buf) - 1)]
	var new_idx = text.font_get_glyph_index(char_fx.font, 1, ch, 0)
	char_fx.glyph_index = new_idx

	return true
