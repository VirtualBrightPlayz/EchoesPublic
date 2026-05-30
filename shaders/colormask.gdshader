// NOTE: Shader automatically converted from Godot Engine 4.5.1.stable.mono's ORMMaterial3D.

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec4 albedo : source_color;
uniform sampler2D texture_emission: source_color, hint_default_black;
uniform vec4 emission : source_color = vec4(0.0);
uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 mask_color : source_color = vec4(1.0);
uniform sampler2D texture_mask : filter_linear_mipmap, repeat_enable;
uniform sampler2D texture_orm : hint_roughness_g, filter_linear_mipmap, repeat_enable;

uniform sampler2D texture_normal : hint_roughness_normal, filter_linear_mipmap, repeat_enable;
uniform float normal_scale : hint_range(-16.0, 16.0);

uniform vec3 uv1_scale;
uniform vec3 uv1_offset;
uniform vec3 uv2_scale;
uniform vec3 uv2_offset;

void vertex() {
	UV = UV * uv1_scale.xy + uv1_offset.xy;
}

void fragment() {
	vec2 base_uv = UV;

	vec4 albedo_tex = texture(texture_albedo, base_uv);
	vec4 mask = texture(texture_mask, base_uv);
	
	ALBEDO = albedo.rgb * albedo_tex.rgb;
	ALBEDO = mix(ALBEDO, ALBEDO * mask_color.rgb, mask.r);

	vec4 orm_tex = texture(texture_orm, base_uv);
	AO = orm_tex.r;
	ROUGHNESS = orm_tex.g;
	METALLIC = orm_tex.b;

	// Normal Map: Enabled
	NORMAL_MAP = texture(texture_normal, base_uv).rgb;
	NORMAL_MAP_DEPTH = normal_scale;
	
	EMISSION = texture(texture_emission, base_uv).rgb*emission.rgb;
}
