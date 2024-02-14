using Silk.NET.OpenGL;

namespace Voxels;

public class Shader : IDisposable
{
    private readonly uint _handle;
    private readonly GL _gl;

    public Shader(GL gl, string vertexPath, string fragmentPath)
    {
        _gl = gl;

        uint vertex = LoadShader(ShaderType.VertexShader, vertexPath);
        uint fragment = LoadShader(ShaderType.FragmentShader, fragmentPath);

        _handle = _gl.CreateProgram();

        _gl.AttachShader(_handle, vertex);
        _gl.AttachShader(_handle, fragment);
        _gl.LinkProgram(_handle);

        _gl.GetProgram(_handle, GLEnum.LinkStatus, out int status);
        if (status == 0)
            throw new Exception($"Program failed to link with error: {_gl.GetProgramInfoLog(_handle)}");

        _gl.DetachShader(_handle, vertex);
        _gl.DetachShader(_handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
    }

    public void Use()
    {
        _gl.UseProgram(_handle);
    }

    public void SetUniform(string name, int value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
            throw new Exception($"{name} uniform not found on shader");
        _gl.Uniform1(location, value);
    }
    
    public void SetUniform(string name, float value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
            throw new Exception($"{name} uniform not found on shader");
        _gl.Uniform1(location, value);
    }
    
    private uint LoadShader(ShaderType type, string path)
    {
        string src = File.ReadAllText(path);
        uint handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, src);
        _gl.CompileShader(handle);
        
        _gl.GetShader(handle, ShaderParameterName.CompileStatus, out int status);
        if (status != (int) GLEnum.True)
            throw new Exception($"Error compiling shader of type {type}, failed with error: {_gl.GetShaderInfoLog(handle)}");

        return handle;
    }

    public void Dispose()
    {
        _gl.DeleteProgram(_handle);
    }
}