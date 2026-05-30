shader_type spatial;
//render_mode depth_draw_never;
render_mode depth_draw_always, cull_disabled;

uniform bool cull_front = false;
uniform sampler2D flow_texture;
uniform sampler2D color_texture : source_color;
uniform sampler2D normal_texture : hint_normal;
uniform vec2 jump_per_phase = vec2(0.25, 0.25);
uniform float texture_tiling = 1.0;
uniform float anim_speed = 1.0;
uniform float flow_strength = 1.0;
uniform float flow_offset = 0.0;
uniform float refraction_strength = 0.25;
uniform vec4 refraction_color : source_color = vec4(1.0);
uniform float water_fog_density = 0.1;
uniform float water_fog_density2 = 0.1;

uniform sampler2D screen_texture : hint_screen_texture;
uniform sampler2D depth_texture : hint_depth_texture;

void vertex() {

}

vec3 flow_uvw(vec2 uv, vec2 flow, vec2 jump, float offset, float tiling, float time, bool flowB) {
	float phase = flowB ? 0.5 : 0.0;
	float progress = fract(time + phase);
	vec3 uvw;
	uvw.xy = uv - flow * (progress + offset);
	uvw.xy *= tiling;
	uvw.xy += phase;
	uvw.xy += (time - progress) * jump;
	uvw.z = 1.0 - abs(1.0 - 2.0 * progress);
	return uvw;
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
	vec3 flow = texture(flow_texture, UV).rgb;
	flow.xy = flow.xy * 2.0 - 1.0;
	flow *= flow_strength;
	float time = TIME * anim_speed;
	vec3 uvwA = flow_uvw(UV, flow.xy, jump_per_phase, flow_offset, texture_tiling, time, false);
	vec3 uvwB = flow_uvw(UV, flow.xy, jump_per_phase, flow_offset, texture_tiling, time, true);
	vec4 colorA = texture(color_texture, uvwA.xy) * uvwA.z;
	vec4 colorB = texture(color_texture, uvwB.xy) * uvwB.z;
	vec4 normalA = texture(normal_texture, uvwA.xy) * uvwA.z;
	vec4 normalB = texture(normal_texture, uvwB.xy) * uvwB.z;
	ALBEDO = colorA.rgb + colorB.rgb;
	NORMAL_MAP = normalA.xyz + normalB.xyz;
	//ALPHA = colorA.a + colorB.a;
	float sceneDepth = texture(depth_texture, SCREEN_UV).r;
	float linearSceneDepth = depth_to_linear(INV_PROJECTION_MATRIX, SCREEN_UV, sceneDepth);
	float fragDepth = FRAGCOORD.z;
	float linearFragDepth = depth_to_linear(INV_PROJECTION_MATRIX, SCREEN_UV, fragDepth);
	float depthDiffer = (linearSceneDepth - linearFragDepth);
	float fogFactor = exp2(water_fog_density * depthDiffer);
	float fogFactor2 = exp2(water_fog_density2 * depthDiffer);
	if (!FRONT_FACING)
	{
		//fogFactor = 1.0 - fogFactor;
		//ALPHA = 0.5;
	}
	//else
	{
		ALPHA = clamp(mix(1.0, 0.0, fogFactor), 0.0, 1.0);
	}
	vec4 refractionColor = texture(screen_texture, (FRAGCOORD.xy + ((NORMAL_MAP - 0.5) * 0.5).xy * refraction_strength * VIEWPORT_SIZE.xy) / VIEWPORT_SIZE.xy);
	refractionColor *= refraction_color;
	//ALBEDO = refractionColor.rgb;
	ALBEDO = mix(ALBEDO, refractionColor.rgb, clamp(fogFactor2, 0.0, 1.0));
	//DEPTH = mix(sceneDepth, fragDepth, fogFactor);
	//SPECULAR = 0.5;
	ROUGHNESS = 0.5;
	//METALLIC = 1.0;
}