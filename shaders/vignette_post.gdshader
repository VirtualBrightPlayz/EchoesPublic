shader_type spatial;
render_mode unshaded, blend_mix, depth_test_disabled, depth_draw_never, ambient_light_disabled, skip_vertex_transform, world_vertex_coords;

instance uniform vec4 main_color : source_color;
instance uniform float amount;

void vertex() {
	POSITION = vec4(VERTEX, 1.0);
}

void fragment() {
	ALBEDO = main_color.rgb;
	float a = length(SCREEN_UV - 0.5) * amount * 2.0;
	ALPHA = clamp(a * main_color.a, 0, 1);
}
