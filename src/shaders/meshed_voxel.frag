#version 330 core

out vec4 out_color;

flat in vec3 normal;

void main() {
	out_color = vec4(normal, 1.0f);
}

