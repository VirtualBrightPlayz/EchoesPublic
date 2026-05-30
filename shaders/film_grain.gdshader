shader_type spatial;
render_mode unshaded, blend_mix, depth_test_disabled, depth_draw_never, ambient_light_disabled, skip_vertex_transform, world_vertex_coords;

uniform sampler2D noise : repeat_enable, filter_nearest;
uniform vec4 color : source_color = vec4(0, 0, 0, 1);
instance uniform float amount : hint_range(0.0, 1.0, 0.01);

void vertex() {
	POSITION = vec4(VERTEX, 1.0);
}

void fragment() {
	ALBEDO.rgb = color.rgb;
	float val = texture(noise, SCREEN_UV + sin(TIME * 100.0)).r;
	float a = length(SCREEN_UV - 0.5) * amount * 2.0;
	ALPHA = val * amount;
}
