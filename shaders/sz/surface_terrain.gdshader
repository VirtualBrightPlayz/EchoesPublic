shader_type spatial;
//render_mode world_vertex_coords;

uniform vec3 up = vec3(0, 1, 0);
uniform vec2 uv_scale = vec2(1, 1);
uniform float power = 32.0;
group_uniforms TextureMain;
uniform sampler2D main_color : source_color;
uniform sampler2D main_roughness : hint_roughness_r;
uniform sampler2D main_normal : hint_normal;
uniform sampler2D main_ao;
group_uniforms TextureSlope;
uniform sampler2D slope_color : source_color;
uniform sampler2D slope_roughness : hint_roughness_r;
uniform sampler2D slope_normal : hint_normal;
uniform sampler2D slope_ao;

varying float prod;

void vertex() {
	prod = 1.0 - clamp(pow(dot(normalize(MODEL_NORMAL_MATRIX * NORMAL), up), power), 0.0, 1.0);
}

void fragment() {
	float d = prod;
	vec2 coords = UV * uv_scale;
	vec4 c = mix(texture(main_color, coords), texture(slope_color, coords), d);
	vec4 n = mix(texture(main_normal, coords), texture(slope_normal, coords), d);
	vec4 rough = mix(texture(main_roughness, coords), texture(slope_roughness, coords), d);
	vec4 ao = mix(texture(main_ao, coords), texture(slope_ao, coords), d);
	ALBEDO = c.rgb;
	NORMAL_MAP = n.rgb;
	ROUGHNESS = rough.r;
	AO = ao.r;
	//ALBEDO = vec3(d);
}
