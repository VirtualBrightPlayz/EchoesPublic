shader_type spatial;

uniform sampler2D albedo_map : repeat_enable;
uniform sampler2D normal_map : repeat_enable;
uniform vec2 albedo_uv_scale = vec2(1);
uniform float roughness = 1.0;

uniform sampler2D snow_map;
uniform sampler2D noise;
uniform sampler2D falloff_map;
uniform float scale = 1.0;
uniform float offset = 0.0;
uniform float noise_scale = 1.0;
uniform float offset_xz_amount = 0.0;


vec3 sample(vec2 uv) {
	float v = texture(snow_map, uv).r * scale + texture(noise, uv).r * noise_scale + offset;
	return vec3(0, v * texture(falloff_map, uv).r, 0);
}

void vertex() {
	vec2 size = 1.0 / vec2(textureSize(snow_map, 0));
	vec3 v = sample(UV.xy);
	vec3 a = sample(UV.xy - vec2(size.x, size.y) * 1.0) - vec3(1, 0, 1);
	vec3 b = sample(UV.xy + vec2(size.x, 0) * 1.0) + vec3(1, 0, 0);
	vec3 c = sample(UV.xy + vec2(0, size.y) * 1.0) - vec3(0, 0, 1);
	VERTEX.xyz += v;
	NORMAL.xyz = normalize(cross(b - a, c - a));
	NORMAL.z *= -1.0;
	TANGENT.xyz = cross(NORMAL.xyz, vec3(0, 1, 0));
	/*
	if (a.y > b.y && a.y > c.y) {
		VERTEX.xz += a.xz * offset_xz_amount;
	} else if (b.y > c.y) {
		VERTEX.xz += b.xz * offset_xz_amount;
	} else {
		VERTEX.xz += c.xz * offset_xz_amount;
	}
	*/
	//VERTEX.xz += NORMAL.xz * offset_xz_amount;
}

void fragment() {
	ALBEDO = texture(albedo_map, UV * albedo_uv_scale).rgb;
	NORMAL_MAP = texture(normal_map, UV * albedo_uv_scale).rgb;
	ROUGHNESS = roughness;
}
