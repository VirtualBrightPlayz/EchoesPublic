shader_type spatial;
render_mode blend_mix,depth_draw_opaque,cull_back,diffuse_burley,specular_schlick_ggx,shadows_disabled;

instance uniform bool debug_disable_fov = false;
instance uniform float fov : hint_range(20, 120) = 70;

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
uniform vec3 uv1_scale = vec3(1,1,1);
uniform vec3 uv1_offset;

float SchlickFresnel(float u) {
    float m = 1.0 - u;
    float m2 = m * m;
    return m2 * m2 * m;
}

float get_effective_fov()
{
	if (debug_disable_fov)
		return 70.0f;
	
	return fov;
}

void vertex() {
	UV=UV*uv1_scale.xy+uv1_offset.xy;
	
	float scale = 1.0 / tan(get_effective_fov() * 0.5 * PI / 180.0); 
	PROJECTION_MATRIX[0][0] = scale / (VIEWPORT_SIZE.x / VIEWPORT_SIZE.y);
	PROJECTION_MATRIX[1][1] = -scale;
	
	POSITION = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX.xyz, 1.0);
	POSITION.z = mix(POSITION.z, POSITION.w, 0.9);
}

void fragment() {
    // calculate fresnel values
    float VdotN = dot(VIEW, NORMAL);
    float fresnel = clamp(SchlickFresnel(VdotN), 0.0, 1.0);

    // sample and mix textures
    vec4 orm = texture(orm_texture, UV);
    vec4 _albedo = texture(albedo_texture, UV) * albedo;

    // apply glass look
    float a = mix(0.001, 1.0, _albedo.a);
    ALPHA = mix(fresnel * edge_color.a, 1.0, a);
    ALBEDO = mix(edge_color.rgb * edge_color.a, _albedo.rgb, a);

    ROUGHNESS = ((orm.g+roughness_brightness)*((orm.g - 0.5) * roughness_contrast + 0.5));
    NORMAL_MAP = texture(normal_map, UV).xyz;
    NORMAL_MAP_DEPTH = normal_strength;

    // function to compensate specular for alpha blend
    // 0.5 * ALPHA^-0.5
    SPECULAR = 0.5 * inversesqrt(ALPHA);
}