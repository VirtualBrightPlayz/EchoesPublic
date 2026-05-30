shader_type spatial;
render_mode diffuse_lambert_wrap, specular_schlick_ggx;

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 albedo : source_color = vec4(1.0, 1.0, 1.0, 1.0);

uniform sampler2D texture_normal : hint_roughness_normal, filter_linear_mipmap, repeat_enable;
uniform float normal_scale : hint_range(-16.0, 16.0);

uniform sampler2D texture_orm : filter_linear_mipmap, repeat_enable;

uniform float specular_intensity;

uniform sampler2D noise_texture : source_color;
uniform float noise_brightness;
uniform float noise_contrast;

uniform vec2 uvnoise_scale = vec2(1.0, 1.0);
uniform vec2 uvnoise_offset;

float bc(float value, float brightness, float contrast) {
    value = (value - 0.5) * contrast + 0.5;
    value += brightness;
    return clamp(value, 0.0, 1.0);
}

void vertex() {
	VERTEX.y += float(.005);
}

void fragment() {
	vec2 base_noise = UV * uvnoise_scale + uvnoise_offset;
	vec2 base_uv = UV;

	vec4 noise_tex = texture(noise_texture, base_noise);
	vec4 orm = texture(texture_orm, base_uv);
	vec4 albedo_tex = texture(texture_albedo, base_uv);
	float noise = float((noise_tex.r+noise_tex.g+noise_tex.b)/3.0);


	float roughness = orm.g;

	ALBEDO = albedo.rgb * albedo_tex.rgb;
	//ALBEDO = vec3(roughness);
	
    ROUGHNESS = roughness;

	NORMAL_MAP = texture(texture_normal, base_uv).xyz;
    NORMAL_MAP_DEPTH = normal_scale;

	ALPHA = clamp(bc(noise, noise_brightness, noise_contrast), 0.0, 1.0)*albedo_tex.a;
	SPECULAR = specular_intensity;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
