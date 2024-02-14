using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using Color = System.Drawing.Color;
using Window = Silk.NET.Windowing.Window;

namespace Voxels;

public static class Program
{
    private static IWindow _window;
    private static GL _gl;

    private static uint _vbo, _ebo, _vao, _shader;

    private static uint _texture;

    private const string VertexShaderSource = """
                                              #version 330 core
                                              layout (location=0) in vec3 aPos;

                                              layout (location = 1) in vec2 aTexCoords;
                                              
                                              out vec2 frag_texCoords;
                                              
                                              void main()
                                              {
                                                  gl_Position = vec4(aPos, 1.0);
                                                  
                                                  frag_texCoords = aTexCoords;
                                              }
                                              """;

    private const string FragmentShaderSource = """
                                                #version 330 core
                                                in vec2 frag_texCoords;

                                                out vec4 out_color;
                                                
                                                uniform sampler2D uTexture;
                                                
                                                void main()
                                                {
                                                    out_color = texture(uTexture, frag_texCoords);
                                                }
                                                """;


    public static void Main()
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "Voxels and such";

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClose;
        _window.Resize += OnResize;
        
        _window.Run();
        _window.Dispose();
    }


    private static unsafe void OnLoad()
    {
        //Set-up input context.
        IInputContext input = _window.CreateInput();
        foreach (IKeyboard key in input.Keyboards)
        {
            key.KeyDown += KeyDown;
        }

        _gl = GL.GetApi(_window);

        _gl.ClearColor(Color.CornflowerBlue);

        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        //Vertex data, uploaded to the VBO.
        float[] vertices =
        [
            //X    Y      Z
            0.5f, 0.5f, 0.0f, 1.0f, 1.0f,
            0.5f, -0.5f, 0.0f, 1.0f, 0.0f,
            -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
            -0.5f, 0.5f, 0.5f, 0.0f, 1.0f
        ];
        
        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* buf = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(uint)), buf,
                BufferUsageARB.StaticDraw);
        }

        //Index data, uploaded to the EBO.
        uint[] indices =
        [
            0, 1, 3,
            1, 2, 3
        ];
        
        _ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* buf = indices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf,
                BufferUsageARB.StaticDraw);
        }

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, VertexShaderSource);
        _gl.CompileShader(vertexShader);

        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int) GLEnum.True)
            throw new Exception($"Vertex Shader failed to compile: {_gl.GetShaderInfoLog(vertexShader)}");

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, FragmentShaderSource);
        _gl.CompileShader(fragmentShader);

        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus != (int) GLEnum.True)
            throw new Exception($"Fragment Shader failed to compile: {_gl.GetShaderInfoLog(fragmentShader)}");

        _shader = _gl.CreateProgram();
        
        _gl.AttachShader(_shader, vertexShader);
        _gl.AttachShader(_shader, fragmentShader);
        _gl.LinkProgram(_shader);

        _gl.GetProgram(_shader, GLEnum.LinkStatus, out int lStatus);
        if (lStatus != (int) GLEnum.True)
            throw new Exception($"Program failed to link: {_gl.GetProgramInfoLog(_shader)}");
        
        _gl.DetachShader(_shader, vertexShader);
        _gl.DetachShader(_shader, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        const uint stride = 3 * sizeof(float) + 2 * sizeof(float);

        const uint positionLoc = 0;
        _gl.EnableVertexAttribArray(positionLoc);
        _gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, stride, null);

        const uint textureLoc = 1;
        _gl.EnableVertexAttribArray(textureLoc);
        _gl.VertexAttribPointer(textureLoc, 2, VertexAttribPointerType.Float, false, stride, (void*) (3 * sizeof(float)));
        
        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        _texture = _gl.GenTexture();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);

        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes("silk.png"), ColorComponents.RedGreenBlueAlpha);

        fixed (byte* ptr = result.Data)
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint) result.Width, (uint) result.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }
        
        _gl.TextureParameter(_texture, TextureParameterName.TextureWrapS, (int) TextureWrapMode.Repeat);
        _gl.TextureParameter(_texture, TextureParameterName.TextureWrapT, (int) TextureWrapMode.Repeat);

        _gl.TextureParameter(_texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TextureParameter(_texture, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
        
        _gl.GenerateMipmap(TextureTarget.Texture2D);
        
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        int location = _gl.GetUniformLocation(_shader, "uTexture");
        _gl.Uniform1(location, 0);
        
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusConstantAlpha);
    }

    private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        if (arg2 == Key.Escape)
        {
            _window.Close();
        }
    }

    private static void OnUpdate(double obj)
    {
    }

    private static unsafe void OnRender(double obj)
    {
        _gl.Clear((uint)ClearBufferMask.ColorBufferBit);
        
        _gl.BindVertexArray(_vao);
        _gl.UseProgram(_shader);
        
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
        
        _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
    }

    private static void OnResize(Vector2D<int> size)
    {
        _gl.Viewport(0, 0, (uint) size.X, (uint) size.Y);
    }
    
    private static void OnClose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteBuffer(_vao);
        _gl.DeleteProgram(_shader);
    }
}