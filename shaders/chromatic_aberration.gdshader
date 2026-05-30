shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable;
uniform float amount_center = 0.0;
uniform vec2 amount_red = vec2(1.0, 0.0);
uniform vec2 amount_green = vec2(1.0, 0.0);
uniform vec2 amount_blue = vec2(0.0, 0.0);

void fragment() {
	vec2 offset = (SCREEN_UV - 0.5);
	vec2 dist = vec2(clamp(length(offset) - amount_center, 0.0, 1.0));
	float r = texture(screen_texture, offset + amount_red * dist + 0.5).r;
	float g = texture(screen_texture, offset + amount_green * dist + 0.5).g;
	float b = texture(screen_texture, offset + amount_blue * dist + 0.5).b;
	vec4 c = vec4(r, g, b, 1.0);
	COLOR.rg = dist.rg;
	COLOR = c;
}
