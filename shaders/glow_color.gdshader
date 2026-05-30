shader_type spatial;

instance uniform vec4 emission;

void fragment() {
	EMISSION.rgb = emission.rgb * emission.a;
}
