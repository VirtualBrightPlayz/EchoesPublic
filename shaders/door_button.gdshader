shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec4 albedo : source_color;
uniform sampler2D texture_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;

uniform float roughness : hint_range(0.0, 1.0);
uniform sampler2D texture_metallic : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D texture_roughness : hint_roughness_r, filter_linear_mipmap_anisotropic, repeat_enable;

uniform float specular : hint_range(0.0, 1.0, 0.01);
uniform float metallic : hint_range(0.0, 1.0, 0.01);

uniform sampler2D texture_emission : source_color, hint_default_black, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float emission_energy : hint_range(0.0, 100.0, 0.01);
instance uniform float emission_energy_multi : hint_range(0.0, 100.0, 0.01);
instance uniform float emission_pulse_amount = 0.0;
uniform vec4 emission_color : source_color;
uniform sampler2D texture_emission_alt : source_color, hint_default_black, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float emission_alt_energy : hint_range(0.0, 100.0, 0.01);

uniform sampler2D texture_normal : hint_roughness_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float normal_scale : hint_range(-16.0, 16.0);

void fragment() {
	vec2 base_uv = UV;

	vec4 albedo_tex = texture(texture_albedo, base_uv);
	ALBEDO = albedo.rgb * albedo_tex.rgb;

	float metallic_tex = texture(texture_metallic, base_uv).r;
	METALLIC = metallic_tex * metallic;
	SPECULAR = specular;

	float roughness_tex = texture(texture_roughness, base_uv).r;
	ROUGHNESS = roughness_tex * roughness;

	NORMAL_MAP = texture(texture_normal, base_uv).rgb;
	NORMAL_MAP_DEPTH = normal_scale;

	vec3 emission_tex = texture(texture_emission, base_uv).rgb;
	vec3 emission_alt_tex = texture(texture_emission_alt, base_uv).rgb;
	float energy = emission_energy_multi * abs(cos(TIME * emission_pulse_amount));
	EMISSION = emission_tex * emission_energy + emission_alt_tex * emission_alt_energy * energy + emission_color.rgb;
}
