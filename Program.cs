using System.Xml;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Voxels;

public static class Program
{
    private static IWindow _window;
    private static GL _gl;

    private static uint _vbo, _ebo, _vao, _shader;

    private const string VertexShaderSource = """
                                              #version 330 core
                                              layout (location=0) in vec4 vPos;

                                              void main()
                                              {
                                                  gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
                                              }
                                              """;

    private const string FragmentShaderSource = """
                                                #version 330 core
                                                out vec4 FragColor;
                                                
                                                void main()
                                                {
                                                    FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
                                                }
                                                """;
    
    //Vertex data, uploaded to the VBO.
    private static readonly float[] Vertices =
    [
        //X    Y      Z
        0.5f,  0.5f, 0.0f,
        0.5f, -0.5f, 0.0f,
        -0.5f, -0.5f, 0.0f,
        -0.5f,  0.5f, 0.5f
    ];

    //Index data, uploaded to the EBO.
    private static readonly uint[] Indices =
    [
        0, 1, 3,
        1, 2, 3
    ];
    
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
        
        _window.Run();
        _window.Dispose();
    }

    

    private static unsafe void OnLoad()
    {
        //Set-up input context.
        IInputContext input = _window.CreateInput();
        for (int i = 0; i < input.Keyboards.Count; i++)
        {
            input.Keyboards[i].KeyDown += KeyDown;
        }

        _gl = GL.GetApi(_window);
        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (void* v = &Vertices[0])
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (Vertices.Length * sizeof(uint)), v, BufferUsageARB.StaticDraw);
        }

        _ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (void* i = &Indices[0])
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (Indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
        }

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, VertexShaderSource);
        _gl.CompileShader(vertexShader);

        string infoLog = _gl.GetShaderInfoLog(vertexShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
            Console.WriteLine($"Error compiling vertex Shader {infoLog}");

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, FragmentShaderSource);
        _gl.CompileShader(fragmentShader);
        
        infoLog = _gl.GetShaderInfoLog(fragmentShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
            Console.WriteLine($"Error compiling fragment Shader {infoLog}");

        _shader = _gl.CreateProgram();
        _gl.AttachShader(_shader, vertexShader);
        _gl.AttachShader(_shader, fragmentShader);
        _gl.LinkProgram(_shader);

        _gl.GetProgram(_shader, GLEnum.LinkStatus, out int status);
        if (status == 0)
        {
            Console.WriteLine($"Error linking shaders {_gl.GetProgramInfoLog(_shader)}");
        }
        
        _gl.DetachShader(_shader, vertexShader);
        _gl.DetachShader(_shader, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
        
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
        _gl.EnableVertexAttribArray(0);
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
        _gl.Clear((uint) ClearBufferMask.ColorBufferBit);
        _gl.BindVertexArray(_vao);
        _gl.UseProgram(_shader);
        _gl.DrawElements(PrimitiveType.Triangles, (uint) Indices.Length, DrawElementsType.UnsignedInt, null);
    }
    
    private static void OnClose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteBuffer(_vao);
        _gl.DeleteProgram(_shader);
    }
}