shader_type canvas_item;
render_mode blend_mix;

//uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float amount = 1.0;
uniform float levels = 256.0;

// Blurs the screen in the X-direction.
void fragment() {
	//sampler2D screen_texture = TEXTURE;
	vec3 col = texture(TEXTURE, SCREEN_UV).xyz * 0.16;
	col += texture(TEXTURE, SCREEN_UV + vec2(TEXTURE_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.15;
	col += texture(TEXTURE, SCREEN_UV + vec2(-TEXTURE_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.15;
	col += texture(TEXTURE, SCREEN_UV + vec2(2.0 * TEXTURE_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.12;
	col += texture(TEXTURE, SCREEN_UV + vec2(2.0 * -TEXTURE_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.12;
	col += texture(TEXTURE, SCREEN_UV + vec2(3.0 * TEXTURE_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.09;
	col += texture(TEXTURE, SCREEN_UV + vec2(3.0 * -TEXTURE_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.09;
	col += texture(TEXTURE, SCREEN_UV + vec2(4.0 * TEXTURE_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.05;
	col += texture(TEXTURE, SCREEN_UV + vec2(4.0 * -TEXTURE_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.05;
	float greyscale = length(col.rgb);//max(col.r, max(col.g, col.b));
	float lower = floor(greyscale * levels) / levels;
	float lowerdiff = abs(greyscale - lower);
	float upper = ceil(greyscale * levels) / levels;
	float upperdiff = abs(greyscale - upper);
	float level = lowerdiff <= upperdiff ? lower : upper;
	float adjustment = level / greyscale;
	COLOR.xyz = col * adjustment;
	//COLOR.w = length(col.xyz);
}