shader_type spatial;
render_mode unshaded, blend_mix;

uniform sampler2D albedo_map : repeat_enable;
uniform vec2 albedo_uv_offset = vec2(0.0, 0.0);
uniform vec4 albedo_color : source_color = vec4(1, 1, 1, 0.5);
uniform float screen_uv_amount = 0.0;

void fragment() {
	vec2 coords = mix(UV + albedo_uv_offset, SCREEN_UV, screen_uv_amount);
	vec4 color = texture(albedo_map, coords) * albedo_color;
	ALBEDO = color.rgb;
	ALPHA = color.a;
}