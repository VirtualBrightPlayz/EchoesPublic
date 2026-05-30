shader_type spatial;
render_mode unshaded, depth_test_inverted;
stencil_mode read, compare_equal, 1;

uniform sampler2D screen : source_color;

void vertex() {
}

void fragment() {
	vec4 c = texture(screen, SCREEN_UV);
	ALBEDO = c.rgb;
	ALPHA = 1.0;
}
