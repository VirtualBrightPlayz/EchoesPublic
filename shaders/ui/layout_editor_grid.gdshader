// adapted from https://madebyevan.com/shaders/grid/
shader_type canvas_item;
render_mode world_vertex_coords;

uniform float size = 1.0;
uniform float offset = 0.0;

varying vec2 world_coords;

void vertex() {
	world_coords = VERTEX;
}

void fragment() {
	vec2 coord = (world_coords.xy + offset) / size;
	vec2 grid = abs(fract(coord - 0.5) - 0.5) / fwidth(coord);
	float line = min(grid.x, grid.y);
	float color = 1.0 - min(line, 1.0);
	COLOR = vec4(color);
}
