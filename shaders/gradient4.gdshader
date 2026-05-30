shader_type canvas_item;

uniform vec3 top_left : source_color = vec3(1);
uniform vec3 bottom_left : source_color = vec3(1);
uniform vec3 bottom_right : source_color = vec3(1);
uniform vec3 top_right : source_color = vec3(1);

void vertex() {
	COLOR.a = 1.0;
	switch (VERTEX_ID) {
		default:
			COLOR = vec4(0, 0, 0, 1);
			break;
		case 0:
			COLOR.rgb = top_left.rgb;
			break;
		case 1:
			COLOR.rgb = bottom_left.rgb;
			break;
		case 2:
			COLOR.rgb = bottom_right.rgb;
			break;
		case 3:
			COLOR.rgb = top_right.rgb;
			break;
	}
}

void fragment() {
}
