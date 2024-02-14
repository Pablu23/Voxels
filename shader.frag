#version 330 core

in vec2 fUv;

out vec4 FragColor;

uniform sampler2D uTexture0;

void main(){
    FragColor = texture(uTexture0, fUv);
}