shader_type spatial;
render_mode diffuse_lambert_wrap, specular_schlick_ggx;

uniform vec2 uvgen_scale = vec2(1.0, 1.0);
uniform vec2 uvgen_offset;
uniform vec2 uvorm_scale = vec2(1.0, 1.0);
uniform vec2 uvorm_offset;

group_uniforms albedo;
uniform vec4 albedo : source_color = vec4(1.0, 1.0, 1.0, 0.0);
uniform sampler2D albedo_texture : source_color, hint_default_white;

uniform sampler2D orm_texture : hint_roughness_g;
uniform float roughness_brightness;
uniform float roughness_contrast;

group_uniforms normal;
uniform float normal_strength : hint_range(-16.0, 16.0) = 1.0;
uniform sampler2D normal_map : hint_normal;

group_uniforms misc;
uniform vec4 edge_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);

float bc(float value, float brightness, float contrast) {
    value = (value - 0.5) * contrast + 0.5;
    value += brightness;
    return clamp(value, 0.0, 1.0);
}

float SchlickFresnel(float u) {
    float m = 1.0 - u;
    float m2 = m * m;
    return m2 * m2 * m;
}

void fragment() {
	
	vec2 base_uv = UV * uvgen_scale + uvgen_offset;
	vec2 base_uv2 = UV * uvorm_scale + uvorm_offset;
	
    // calculate fresnel values
    float VdotN = dot(VIEW, NORMAL);
    float fresnel = clamp(SchlickFresnel(VdotN), 0.0, 1.0);

    // sample and mix textures
    vec4 orm = texture(orm_texture, base_uv2);
    vec4 _albedo = texture(albedo_texture, base_uv) * albedo;

    // apply glass look
    float a = mix(0.001, 1.0, _albedo.a);
    ALPHA = mix(fresnel * edge_color.a, 1.0, a);
    ALBEDO = mix(edge_color.rgb * edge_color.a, _albedo.rgb, a);

    ROUGHNESS = clamp(bc(orm.g, roughness_brightness, roughness_contrast), 0.0, 1.0);
    NORMAL_MAP = texture(normal_map, base_uv).xyz;
    NORMAL_MAP_DEPTH = normal_strength;

    // function to compensate specular for alpha blend
    // 0.5 * ALPHA^-0.5
    SPECULAR = 0.5 * inversesqrt(ALPHA);
	
}