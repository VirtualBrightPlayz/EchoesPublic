shader_type spatial;
render_mode unshaded, ambient_light_disabled, blend_add;

uniform vec4 emission_color : source_color = vec4(1.0,1.0,1.0,1.0);
uniform sampler2D noise_tex : repeat_enable;
uniform sampler2D main_tex : hint_screen_texture;
uniform float strength_main = 5.0;
uniform vec2 time_speed = vec2(1.0);

uniform sampler2D texture_displacement;
uniform vec3 scale_displacement;
uniform vec2 tile_displacement = vec2(1.0,1.0);

float SchlickFresnel(float u) {
    float m = 1.0 - u;
    float m2 = m * m;
    return m2 * m2 * m;
}

void fragment() {
	float VdotN = dot(VIEW, NORMAL)*.2;
    float fresnel = clamp(SchlickFresnel(VdotN), 0.0, 1.0);
	
	vec4 noise = texture(noise_tex, UV + vec2(TIME * time_speed.x, TIME * time_speed.y))*strength_main+SchlickFresnel(1);
	vec4 c = texture(main_tex, SCREEN_UV + noise.x*0.01);
	ALBEDO = clamp((c.rgb+(pow(fresnel,5.0)*5.0)), 0.0, 1.0)*vec3(emission_color.r, emission_color.g, emission_color.b);
	ALPHA = clamp(((fresnel)/.5), 0.0, 1.0)*float(emission_color.a);
}


void vertex() {
	
	
	float uvx = UV.x*tile_displacement.x+(TIME*time_speed.x);
	float uvy = UV.y*tile_displacement.y+(TIME*time_speed.y);
	vec2 uv = vec2(uvx,uvy);
	vec3 displacement = (texture(texture_displacement,uv).rgb-0.5)*scale_displacement;
	
	
	VERTEX.x += displacement.r;
	VERTEX.y += displacement.g;
	VERTEX.z += displacement.b;
	}

