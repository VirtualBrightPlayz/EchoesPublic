shader_type canvas_item;
render_mode unshaded, blend_add;

uniform sampler2DArray main_tex;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform int count;

//void vertex() {
	//POSITION = vec4(VERTEX, 1.0);
//}

void fragment() {
	vec3 c = -texture(screen_texture, SCREEN_UV.xy).rgb;
	vec3 c2 = vec3(0);
	for (int i = 0; i < count; i++) {
		 c2 += texture(main_tex, vec3(SCREEN_UV.xy, float(i))).rgb;
	}
	c2 /= float(count + 0);
	COLOR.rgb = c + c2;
	COLOR.a = 0.5;
	//COLOR.a = 0.5;
	//ALPHA = 1.0;
	//COLOR.a = 0.5;
	//ALPHA = 0.05;
}
