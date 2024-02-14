using System.Numerics;
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

    private const int Width = 800, Height = 700;
    
    private static BufferObject<float> _vbo;
    private static BufferObject<uint> _ebo;
    private static VertexArrayObject<float, uint> _vao;
    private static Texture _texture;
    private static Shader _shader;

    private static Vector3 _cameraPos = new Vector3(0.0f, 0.0f, 3.0f);
    private static Vector3 _cameraTarget = Vector3.Zero;
    private static Vector3 _cameraDir = Vector3.Normalize(_cameraPos - _cameraTarget);
    private static Vector3 _cameraRight = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, _cameraDir));
    private static Vector3 _cameraUp = Vector3.Cross(_cameraDir, _cameraRight);
    
    private static Transform[] _transforms = new Transform[4];
    
    private static readonly float[] Vertices =
    {
        //X    Y      Z     U   V
        -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
        0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
        0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,

        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
        -0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,

        -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        -0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

        0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        0.5f, -0.5f, -0.5f,  1.0f, 1.0f,
        0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
        0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,

        -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
        0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,
        -0.5f,  0.5f, -0.5f,  0.0f, 1.0f
    };

    private static readonly uint[] Indices =
    {
        0, 1, 3,
        1, 2, 3
    };
    
    public static void Main()
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "Voxels and such";

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Closing += OnClose;
        
        _window.Run();
        _window.Dispose();
    }


    private static void OnLoad()
    {
        IInputContext input = _window.CreateInput();
        foreach (IKeyboard keyboard in input.Keyboards)
        {
            keyboard.KeyDown += KeyDown;
        }

        _gl = GL.GetApi(_window);

        _ebo = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);

        _shader = new Shader(_gl, "shader.vert", "shader.frag");
        _texture = new Texture(_gl, "silk.png");

        _transforms[0] = new Transform
        {
            Position = new Vector3(0.5f, 0.5f, 0f)
        };

        _transforms[1] = new Transform
        {
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1f)
        };
        //Scaling.
        _transforms[2] = new Transform
        {
            Scale = 0.5f
        };
        //Mixed transformation.
        _transforms[3] = new Transform
        {
            Position = new Vector3(-0.5f, 0.5f, 0f),
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1f),
            Scale = 0.5f
        };
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
        _gl.Enable(EnableCap.DepthTest);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        _vao.Bind();
        _texture.Bind();
        _shader.Use();
        _shader.SetUniform("uTexture0", 0);

        float diff = (float)(_window.Time * 100);

        Matrix4x4 model = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(diff)) *
                          Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(diff));

        var view = Matrix4x4.CreateLookAt(_cameraPos, _cameraTarget, _cameraUp);
        var projection =
            Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), Width / Height, 0.1f, 100f);
        
        _shader.SetUniform("uModel", model);
        _shader.SetUniform("uView", view);
        _shader.SetUniform("uProjection", projection);
        
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }

    private static void OnResize(Vector2D<int> size)
    {
        _gl.Viewport(0, 0, (uint) size.X, (uint) size.Y);
    }
    
    private static void OnClose()
    {
        _vbo.Dispose();
        _ebo.Dispose();
        _vao.Dispose();
        _shader.Dispose();
        _texture.Dispose();
    }
}