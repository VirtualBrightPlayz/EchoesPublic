shader_type spatial;
render_mode unshaded, blend_mix;

uniform int color_samples : hint_range(0, 128, 1) = 0;
uniform int overlay_depth_samples : hint_range(0, 64, 1) = 1;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform sampler2D overlay_texture;
uniform float debug_amount : hint_range(0.0, 1.0) = 0.0;
uniform float multi = 1.0;
uniform float depth_add = 0.0;
uniform float depth_multi = 1.0;

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
	depth_amount = max(depth_amount, 0.0);
	return depth_amount;
}

float get_depth(vec2 uv, mat4 inv_proj) {
	//sampler2D tex = depth_texture;
	float depth = texture(depth_texture, uv).x;
	return apply_depth(depth, uv, inv_proj);
}

float get_blur_depth(vec2 uv, vec2 blur, mat4 inv_proj) {
	float depth = 0.0;
	depth += apply_depth(get_color(uv, blur).x, uv, inv_proj);
	return depth;
}

void fragment() {
	float depth_amount = get_depth(SCREEN_UV, INV_PROJECTION_MATRIX);
	vec2 size = VIEWPORT_SIZE;
	vec2 amount = vec2(depth_amount) / (size * size);
	vec3 og = texture(screen_texture, SCREEN_UV).xyz;
	vec3 col = vec3(0);
	float sum = 0.0;
	for (int i = 0; i < color_samples; i++) {
		float mul = 1.0 / float(i + 2);
		sum += mul;
		col += get_color(SCREEN_UV, float(i + 1) * multi * size * amount) * mul;
	}
	col += og * (1.0 - sum);
	
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