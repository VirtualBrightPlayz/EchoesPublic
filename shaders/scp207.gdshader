shader_type canvas_item;
render_mode blend_add;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform sampler2D noise : repeat_enable;
uniform float amount = 1.0;
uniform float speed = 1.0;
uniform vec3 color : source_color = vec3(1.0);

float get_amount(float offset, float freq) {
	return sin(TIME * speed + offset + freq) * 0.5;
}

vec2 get_pos(float angle) {
	float x = cos(angle);
	float y = sin(angle);
	return vec2(x, y) * 0.5 + 0.5;
}

float get_angle(vec2 pos) {
	pos -= 0.5;
	pos *= 2.0;
	float a = pos.x < 0.0 ? PI : 0.0;
	return mod(atan(pos.y / pos.x) + a, PI * 2.0);
}

void fragment() {
	float fall_0 = 1.0;
	float fall_1 = abs(0.5 - UV.x) * 0.5;
	float fall_2 = distance(UV, vec2(0.5)) - amount;
	float a = get_amount(0.0, 1.0);
	float ang = get_angle(UV);
	float in_ang = mod(TIME, PI * 2.0);
	vec2 pos = get_pos(in_ang);
	float fall_3 = distance(UV, pos);
	if (in_ang < ang - PI) {
		in_ang += PI * 2.0;
	}
	if (in_ang > ang + PI) {
		in_ang -= PI * 2.0;
	}
	float fall_4 = clamp(1.0 - distance(in_ang, ang) * 2.0, 0.0, 1.0);
	float fall_5 = texture(noise, vec2(ang / (PI * 2.0) + mod(TIME * speed, 1.0), mod(TIME * speed + 0.25, 1.0))).r;
	COLOR.rgb = clamp(vec3(fall_5 * fall_2) * color, 0.0, 1.0);
}
