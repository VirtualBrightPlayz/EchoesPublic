shader_type spatial;
render_mode unshaded, blend_mix, depth_test_disabled, depth_draw_never, ambient_light_disabled, skip_vertex_transform, world_vertex_coords;

uniform vec4 main_color : source_color;

void vertex() {
	POSITION = vec4(VERTEX, 1.0);
}

void fragment() {
	ALBEDO = main_color.rgb;
	ALPHA = main_color.a;
}
