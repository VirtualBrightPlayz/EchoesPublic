shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, depth_test_disabled;

uniform sampler2D main_texture : repeat_enable, filter_nearest, hint_default_transparent;

void vertex() {
	//POSITION = vec4(VERTEX, 1.0);
}

void fragment() {
	vec4 c = texture(main_texture, UV);
	ALBEDO = c.rgb;
	ALPHA = c.a;
}

