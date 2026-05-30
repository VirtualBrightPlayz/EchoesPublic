shader_type spatial;
render_mode cull_disabled;

uniform sampler2D main_tex : repeat_enable;
uniform float speed = 1.0;

void fragment() {
	vec2 offset = vec2(0.0, TIME * speed * 5.0);
	vec2 uv = UV;
	vec4 c = texture(main_tex, uv + offset);
	ALBEDO.rgb = c.rgb;
	ALPHA = c.a;
	EMISSION.rgb = c.rgb;
}
