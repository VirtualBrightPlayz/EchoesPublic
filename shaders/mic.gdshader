shader_type canvas_item;

uniform float progress = 0.0;

void fragment() {
	if (UV.y < 1.0 - progress)
		COLOR = vec4(0.0);
	else
		COLOR = texture(TEXTURE, UV);
}