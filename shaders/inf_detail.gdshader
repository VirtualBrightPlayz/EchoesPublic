shader_type spatial;
render_mode unshaded, blend_mix, shadows_disabled;

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest, repeat_enable;
uniform sampler2D albedo : source_color, repeat_enable, filter_linear_mipmap;
uniform sampler3D noise : repeat_enable;
uniform sampler2D noise2 : repeat_enable;
uniform float size = 64.0;
uniform float scale = 1.0;

varying mat4 INV_MODELVIEW_MATRIX;
varying vec3 world_pos;

void vertex() {
	INV_MODELVIEW_MATRIX = inverse(MODELVIEW_MATRIX);
	world_pos = VERTEX;
}

void fragment() {
	//vec3 world = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float n = texture(noise, world_pos * size).r;
	vec3 dir = (texture(noise2, UV * size).xyz - 0.5) * 2.0 * scale;
	vec2 offset = vec2(dir.x, dir.y) * dir.z / vec2(textureSize(albedo, 0));
	vec4 c = texture(albedo, UV + offset);
	ALBEDO.rgb = c.rgb;
	ALPHA = 0.0;
	//ALBEDO.rgb = dir;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
