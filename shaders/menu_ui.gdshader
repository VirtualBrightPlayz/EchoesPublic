shader_type canvas_item;

uniform sampler2D main_tex;
uniform float mul;
uniform float add;

void fragment() {
	COLOR.rgb = texture(main_tex, UV).rgb * mul + add;
}
