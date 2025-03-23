#version 330 core

out vec3 color;

vec3[] positions = vec3[](
	vec3(-1.0, -1.0, 0.0),
	vec3(0.0, 1.0, 0.0),
	vec3(1.0, 0.0, 0.0)
);

void main()
{
	color = positions[gl_VertexID];
    gl_Position = vec4(positions[gl_VertexID], 1.0);
}
