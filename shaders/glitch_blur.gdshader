shader_type canvas_item;
render_mode blend_disabled;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform sampler2D noise_texture : hint_default_white, repeat_enable, filter_nearest;
uniform float amount = 0.0;
uniform float frequency_0 = 1.0;
uniform float frequency_1 = 1.0;
uniform float speed = 1.0;
uniform float steps_0 = 255.0;
uniform float steps_1 = 255.0;
uniform float noise_steps = 255.0;

float truncstep(float input, float steps) {
	//return mod(input, steps);
	return trunc(input * steps) / steps;
}

float get_amount(float offset, float freq, vec2 uv, float steps, float n) {
	float noise_amount = truncstep(texture(noise_texture, uv).r, noise_steps);
	float input_wave = truncstep(TIME * speed + offset + uv.y * freq, steps);// * noise_amount;
	float wave = sin(input_wave) * 0.5;
	return wave;
}

void fragment() {
	float fall_0 = 1.0;
	float fall_1 = abs(0.5 - SCREEN_UV.x) * 0.5;
	float r_amount = get_amount(0.0, frequency_0, SCREEN_UV, steps_0, 0.0) * fall_0 + get_amount(0.0, frequency_1, SCREEN_UV, steps_1, 1.0) * fall_1;
	float g_amount = get_amount(0.25, frequency_0, SCREEN_UV, steps_0, 0.0) * fall_0 + get_amount(0.25, frequency_1, SCREEN_UV, steps_1, 1.0) * fall_1;
	float b_amount = get_amount(0.75, frequency_0, SCREEN_UV, steps_0, 0.0) * fall_0 + get_amount(0.75, frequency_1, SCREEN_UV, steps_1, 1.0) * fall_1;
	COLOR.r = texture(screen_texture, SCREEN_UV + vec2(r_amount, 0.0) * amount).r;
	COLOR.g = texture(screen_texture, SCREEN_UV + vec2(g_amount, 0.0) * amount).g;
	COLOR.b = texture(screen_texture, SCREEN_UV + vec2(b_amount, 0.0) * amount).b;
	//COLOR.a = length(COLOR.rgb);
}
