﻿#version 330 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 fNormal;
out vec3 fPos;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0f);
    fPos = vec3(uModel * vec4(vPos, 1.0));
    fNormal = mat3(transpose(inverse(uModel))) * vNormal;
}