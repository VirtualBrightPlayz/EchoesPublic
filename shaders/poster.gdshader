shader_type spatial;
render_mode diffuse_lambert_wrap, specular_schlick_ggx;

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 albedo : source_color = vec4(1.0, 1.0, 1.0, 1.0);

uniform sampler2D texture_normal : hint_roughness_normal, filter_linear_mipmap, repeat_enable;
uniform float normal_scale : hint_range(-16.0, 16.0);

uniform sampler2D texture_orm : hint_roughness_g, filter_linear_mipmap, repeat_enable;
uniform float roughness_brightness;
uniform float roughness_contrast;

uniform float albedooverlayroughness_brightness;
uniform float albedooverlayroughness_contrast;

uniform float specular_intensity;

uniform float overlay_albedo_strength : hint_range(0.0, 1.0) = float(0);

uniform sampler2D ripping_texture : source_color;
uniform float ripping_brightness=.48;
uniform float ripping_contrast=1.0;

uniform vec2 uvnormal_scale = vec2(1.0, 1.0);
uniform vec2 uvnormal_offset;

uniform vec2 uvorm_scale = vec2(1.0, 1.0);
uniform vec2 uvorm_offset;

uniform vec2 uvripping_scale = vec2(1.0, 1.0);
uniform vec2 uvripping_offset;

float bc(float value, float brightness, float contrast) {
    value = (value - 0.5) * contrast + 0.5;
    value += brightness;
    return clamp(value, 0.0, 1.0);
}

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 base_uvnormal = UV * uvnormal_scale + uvnormal_offset;
	vec2 base_uvorm = UV * uvorm_scale + uvorm_offset;
	vec2 base_ripping = UV * uvripping_scale + uvripping_offset;
	vec2 base_uv = UV;

	vec4 ripping_tex = texture(ripping_texture, base_ripping);
	vec4 orm = texture(texture_orm, base_uvorm);
	vec4 albedo_tex = texture(texture_albedo, base_uv);
	float base = bc(orm.g, roughness_brightness, roughness_contrast);
	base = clamp(base, 0.0, 1.0);
	float pb = float(1.0-((albedo_tex.r+albedo_tex.g+albedo_tex.b)/3.0));
	pb = clamp(pb, 0.0, 1.0);
	float blend = bc(pb, albedooverlayroughness_brightness, albedooverlayroughness_contrast);
	
	float ripping = float((ripping_tex.r+ripping_tex.g+ripping_tex.b)/3.0);


	float roughness = (base+((blend)*overlay_albedo_strength));

	ALBEDO = albedo.rgb * albedo_tex.rgb;
	//ALBEDO = vec3(roughness);
	
    ROUGHNESS = roughness;

	NORMAL_MAP = texture(texture_normal, base_uvnormal).xyz;
    NORMAL_MAP_DEPTH = normal_scale;

	ALPHA = bc(ripping, ripping_brightness, ripping_contrast);
	ALPHA_SCISSOR_THRESHOLD = float(.5);
	SPECULAR = specular_intensity;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
