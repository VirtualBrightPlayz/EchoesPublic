shader_type canvas_item;
render_mode blend_mix;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
//uniform float amount = 1.0;
uniform float multi = 1.0;
uniform sampler2D amount_texture;

// Blurs the screen in the X-direction.
void fragment() {
	float amount = texture(amount_texture, SCREEN_UV).r * multi;
	vec3 col = texture(screen_texture, SCREEN_UV).xyz * 0.16;
	col += texture(screen_texture, SCREEN_UV + vec2(SCREEN_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.15;
	col += texture(screen_texture, SCREEN_UV + vec2(-SCREEN_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.15;
	col += texture(screen_texture, SCREEN_UV + vec2(2.0 * SCREEN_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.12;
	col += texture(screen_texture, SCREEN_UV + vec2(2.0 * -SCREEN_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.12;
	col += texture(screen_texture, SCREEN_UV + vec2(3.0 * SCREEN_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.09;
	col += texture(screen_texture, SCREEN_UV + vec2(3.0 * -SCREEN_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.09;
	col += texture(screen_texture, SCREEN_UV + vec2(4.0 * SCREEN_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.05;
	col += texture(screen_texture, SCREEN_UV + vec2(4.0 * -SCREEN_PIXEL_SIZE.x, 0.0) * amount).xyz * 0.05;
	COLOR.xyz = col;
	COLOR.w = length(col.xyz);
}