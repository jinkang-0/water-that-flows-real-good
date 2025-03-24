#version 330 core

uniform mat4 view_proj;

out vec3 color;

vec3[] positions = vec3[](
	vec3(0.0, 1.0, 0.0),
	vec3(1.0, 1.0, 0.0),
	vec3(0.0, 1.0, 1.0)
);

void main()
{
	color = vec3(positions[gl_VertexID].x, 0.0, positions[gl_VertexID].z);
    gl_Position = view_proj * vec4(positions[gl_VertexID], 1.0);
}
