shader_type spatial;

uniform sampler2D albedo : filter_nearest_mipmap, repeat_enable;
uniform vec4 albedo_tint : source_color;
uniform float specular = .5;
uniform float roughness = .2;
uniform sampler2D emission : filter_nearest_mipmap, repeat_enable;
uniform vec4 emission_color : source_color;
uniform vec2 fb_size = vec2(4.0, 4.0);
uniform float fb_speed = 10.0;

vec2 flipbook(vec2 uv, vec2 size, float progress){
	progress = floor( mod(progress, (size.x * size.y)) );
	vec2 frame_size = vec2(1.0, 1.0) / vec2(size.x, size.y);
	vec2 frame = fract(uv / size) + frame_size;
	
	frame.x += ( (progress / size.x) - frame_size.x * floor(progress/size.x) * size.x ) - frame_size.x;
	frame.y += (frame_size.y * floor(progress/size.x) ) - frame_size.y ;
	
	return frame;
}


void fragment()
{
	vec2 flip_uv = flipbook(UV, fb_size, TIME * fb_speed);
	ALBEDO = texture(albedo, flip_uv).rgb*albedo_tint.rgb;
	EMISSION = texture(emission, flip_uv).rgb*emission_color.rgb;
	SPECULAR = specular;
	ROUGHNESS = roughness;
}
