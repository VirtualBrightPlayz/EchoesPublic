shader_type spatial;
render_mode unshaded, blend_mix;

uniform int color_samples : hint_range(0, 128, 1) = 0;
uniform int overlay_depth_samples : hint_range(0, 64, 1) = 1;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform sampler2D overlay_texture;
uniform float debug_amount : hint_range(0.0, 1.0) = 0.0;
uniform float multi = 1.0;
uniform float blur_multi = 1.0;
uniform float depth_add = 0.0;
uniform float depth_multi = 1.0;
uniform float sigma = 0.8;

void vertex() {
	POSITION = vec4(VERTEX, 1.0);
}

vec3 get_color(vec2 uv, vec2 blur) {
	vec3 col = vec3(0);
	if (length(blur) <= 0.0) {
		return texture(screen_texture, uv).xyz;
	}
	col += texture(screen_texture, uv + vec2(blur.x, 0.0)).xyz * 0.125;
	col += texture(screen_texture, uv + vec2(-blur.x, 0.0)).xyz * 0.125;
	col += texture(screen_texture, uv + vec2(0.0, blur.y)).xyz * 0.125;
	col += texture(screen_texture, uv + vec2(0.0, -blur.y)).xyz * 0.125;

	col += texture(screen_texture, uv + vec2(blur.x, blur.y)).xyz * 0.125;
	col += texture(screen_texture, uv + vec2(-blur.x, -blur.y)).xyz * 0.125;
	col += texture(screen_texture, uv + vec2(blur.x, -blur.y)).xyz * 0.125;
	col += texture(screen_texture, uv + vec2(-blur.x, blur.y)).xyz * 0.125;
	return col;
}

float apply_depth(float depth, vec2 uv, mat4 inv_proj) {
    vec4 upos = inv_proj * vec4(uv * 2.0 - 1.0, depth, 1.0);
	float depth_amount = upos.w * depth_multi + depth_add;
	depth_amount = min(depth_amount, 0.0);
	return depth_amount;
}

float get_depth(vec2 uv, mat4 inv_proj) {
	float depth = texture(depth_texture, uv).x;
	return apply_depth(depth, uv, inv_proj);
}

float get_blur_depth(vec2 uv, vec2 blur, mat4 inv_proj) {
	float depth = 0.0;
	depth += apply_depth(get_color(uv, blur).x, uv, inv_proj);
	return depth;
}

vec3 get_blur_color(vec2 uv, mat4 inv_proj, vec2 blur, float o_value) {
	vec3 col = vec3(0);
	vec2 size = vec2(textureSize(screen_texture, 0));
	vec2 blur_pos = (blur / size);
	float depth = clamp(get_depth(uv + blur_pos, inv_proj), 0.0, 1.0);
	col = texture(screen_texture, uv + blur_pos * depth).xyz;
	float base_val = 1.0 / pow(2.0 * PI * o_value, 2.0);
	float exp_val = -((blur_pos.x * blur_pos.x + blur_pos.y * blur_pos.y) / pow(2.0 * o_value, 2.0));
	float final = base_val * pow(E, exp_val);
	return col * final;
}

void fragment() {
	float depth_amount = get_depth(SCREEN_UV, INV_PROJECTION_MATRIX);
	vec2 size = VIEWPORT_SIZE;
	float amount = clamp(depth_amount, 0.0, 1.0);
	vec3 og = texture(screen_texture, SCREEN_UV).xyz;
	vec3 col = vec3(0);
	float max_val = pow(float(color_samples * 2), 2.0);
	float max_val_inv = 1.0 / max_val;
	for (int y = -color_samples; y <= color_samples; y++) {
		for (int x = -color_samples; x <= color_samples; x++) {
			col += get_blur_color(SCREEN_UV, INV_PROJECTION_MATRIX, vec2(float(x), float(y)) * blur_multi, sigma) * multi * max_val_inv;
		}
	}
	col += og * (1.0 - amount);
	col = mix(og, col, amount);

	if (overlay_depth_samples > 0) {
		vec3 overlay = texture(overlay_texture, SCREEN_UV).xyz * 0.25;
		float overlay_depth = 0.0;
		for (int i = 0; i < overlay_depth_samples; i++) {
			overlay_depth += get_blur_depth(SCREEN_UV, float(i * 16) / size, INV_PROJECTION_MATRIX);
		}
		overlay_depth /= float(overlay_depth_samples);
		col = mix(col, overlay, overlay_depth);
	}

	ALBEDO.xyz = mix(col, vec3(depth_amount), debug_amount);
}