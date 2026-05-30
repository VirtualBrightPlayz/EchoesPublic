shader_type spatial;
//render_mode blend_add, depth_draw_never, cull_back, specular_disabled, ambient_light_disabled, fog_disabled;
render_mode unshaded, depth_draw_never, cull_back, blend_add, ambient_light_disabled, fog_disabled;

uniform vec4 albedo : source_color = vec4(1, 1, 1, 1);
uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D depth_texture : hint_depth_texture;

uniform float angle_fade = 2.0;

uniform float distance_fade_min : hint_range(0.0, 4096.0, 0.01) = 0.5;
uniform float distance_fade_max : hint_range(0.0, 4096.0, 0.01) = 2.0;

uniform vec3 uv1_scale;
uniform vec3 uv1_offset;
uniform vec3 uv2_scale;
uniform vec3 uv2_offset;

varying vec3 model_pos;
instance uniform float mesh_size = 1.0;

void vertex() {
	UV = UV * uv1_scale.xy + uv1_offset.xy;
	model_pos = VERTEX;
}

float depth_to_linear(mat4 inv_proj, vec2 uv, float depth) {
	#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(uv, depth) * 2.0 - 1.0;
	#else
	vec3 ndc = vec3(uv * 2.0 - 1.0, depth);
	#endif
	vec4 view = inv_proj * vec4(ndc, 1.0);
	view.xyz /= view.w;
	return view.z;
}

void fragment() {
	vec2 base_uv = UV;
	
	vec4 albedo_tex = texture(texture_albedo, base_uv);
	ALBEDO = albedo.rgb * albedo_tex.rgb;
	ALPHA = albedo.a * albedo_tex.a;
	
	ALPHA *= clamp(pow(dot(NORMAL, VIEW), angle_fade), 0.0, 1.0);
	
	//EMISSION += clamp(dot(VIEW, NORMAL) * angle_fade, 0.0, 1.0);
	
	// Distance Fade: Pixel Alpha
	ALPHA *= clamp(smoothstep(distance_fade_min, distance_fade_max, length(VERTEX)), 0.0, 1.0);
	
	ALPHA *= clamp((model_pos.y + mesh_size * 0.5) / mesh_size, 0.0, 1.0);
	
	float scenedepth = depth_to_linear(INV_PROJECTION_MATRIX, SCREEN_UV, texture(depth_texture, SCREEN_UV).r);
	float fragdepth = depth_to_linear(INV_PROJECTION_MATRIX, SCREEN_UV, FRAGCOORD.z);
	ALPHA *= clamp((fragdepth - scenedepth) / mesh_size, 0.0, 1.0);
}
