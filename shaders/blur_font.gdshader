shader_type canvas_item;
render_mode blend_mix;

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_nearest;

void fragment() {
	float scl = 0.01;
	vec4 col = texture(screen_tex, SCREEN_UV);
	//col *= texture(screen_tex, SCREEN_UV + vec2(scl, 0.0)) * 0.5;
	//col *= texture(screen_tex, SCREEN_UV + vec2(-scl, 0.0)) * 0.5;
	//col /= 3.0;
	COLOR = col;
}