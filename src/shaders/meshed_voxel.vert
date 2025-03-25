#version 330 core

layout (location = 0) in vec3 in_pos;
layout (location = 1) in vec3 in_normal;

uniform mat4 view_proj;

flat out vec3 normal;

void main() {
	normal = 0.5 * in_normal + 0.5;
	gl_Position = view_proj * vec4(in_pos, 1.0);
}

