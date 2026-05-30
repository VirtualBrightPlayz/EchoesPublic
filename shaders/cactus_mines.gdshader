shader_type spatial;

instance uniform float ratio;
instance uniform int max_instances = 1;
instance uniform int min_instances = 1;
uniform sampler2D curve : repeat_disable;
uniform int area = 8;
uniform sampler2D albedo_texture : source_color, hint_default_white;

void vertex() {
	float cur_ratio = (float(min_instances) - float(INSTANCE_ID) - 1.0) / float(area) + (ratio - float(min_instances)) / float(area);
	float r = texture(curve, vec2(cur_ratio)).r;
	VERTEX *= r;
}

void fragment() {
	ALBEDO = texture(albedo_texture, UV).rgb;
}
