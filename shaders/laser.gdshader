shader_type spatial;
render_mode blend_mix;

uniform sampler2D noise;
uniform float emission = 1.0;
uniform float speed = 0.5;

void vertex() {
	//UV = SCREEN_UV;
}

void fragment() {
	ALBEDO = vec3(1.0, 0.0, 0.0);
	ALPHA = texture(noise, UV + vec2(sin(TIME * speed), TIME * -speed)).r;
	EMISSION = vec3(ALPHA * emission, 0.0, 0.0);
}
