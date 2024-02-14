using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using Color = System.Drawing.Color;
using Window = Silk.NET.Windowing.Window;

// ReSharper disable FieldCanBeMadeReadOnly.Local
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Voxels;

public static class Program
{
    private static IWindow _window;
    private static GL _gl;
    private static IKeyboard? _primaryKeyboard;
    
    private const int Width = 800, Height = 700;

    private static BufferObject<float> _vbo;
    private static BufferObject<uint> _ebo;
    private static VertexArrayObject<float, uint> _vao;
    private static Texture _texture;
    private static Shader _shader;

    private static Vector3 _cameraPos = new(0.0f, 0.0f, 3.0f);
    private static Vector3 _cameraFront = new(0.0f, 0.0f, -1.0f);
    private static Vector3 _cameraUp = Vector3.UnitY;
    private static Vector3 _cameraDir = Vector3.Zero;
    private static float _cameraYaw = -90f;
    private static float _cameraPitch = 0f;
    private static float _cameraZoom = 45f;

    private static Vector2 _lastMousePos;
    
    private static readonly float[] Vertices =
    [
        //X    Y      Z     U   V
        -0.5f, -0.5f, -0.5f, 0.0f, 1.0f,
        0.5f, -0.5f, -0.5f, 1.0f, 1.0f,
        0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
        0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
        -0.5f, 0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f, -0.5f, 0.0f, 1.0f,

        -0.5f, -0.5f, 0.5f, 0.0f, 1.0f,
        0.5f, -0.5f, 0.5f, 1.0f, 1.0f,
        0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
        0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
        -0.5f, 0.5f, 0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f, 0.5f, 0.0f, 1.0f,

        -0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
        -0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f, 0.5f, 0.0f, 1.0f,
        -0.5f, 0.5f, 0.5f, 1.0f, 1.0f,

        0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
        0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
        0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        0.5f, -0.5f, 0.5f, 0.0f, 1.0f,
        0.5f, 0.5f, 0.5f, 1.0f, 1.0f,

        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        0.5f, -0.5f, -0.5f, 1.0f, 0.0f,
        0.5f, -0.5f, 0.5f, 1.0f, 1.0f,
        0.5f, -0.5f, 0.5f, 1.0f, 1.0f,
        -0.5f, -0.5f, 0.5f, 0.0f, 1.0f,
        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,

        -0.5f, 0.5f, -0.5f, 0.0f, 0.0f,
        0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
        0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
        0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
        -0.5f, 0.5f, 0.5f, 0.0f, 1.0f,
        -0.5f, 0.5f, -0.5f, 0.0f, 0.0f
    ];


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


    private static void OnLoad()
    {
        IInputContext input = _window.CreateInput();
#pragma warning disable CA1826
        _primaryKeyboard = input.Keyboards.FirstOrDefault();
#pragma warning restore CA1826
        if (_primaryKeyboard is not null)
        {
            _primaryKeyboard.KeyDown += KeyDown;
        }

        for (int i = 0; i < input.Mice.Count; i++)
        {
            input.Mice[i].Cursor.CursorMode = CursorMode.Raw;
            input.Mice[i].MouseMove += OnMouseMove;
            input.Mice[i].Scroll += OnMouseWheel;
        }

        _gl = GL.GetApi(_window);

        _ebo = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);

        _shader = new Shader(_gl, "shader.vert", "shader.frag");
        _texture = new Texture(_gl, "silk.png");
    }

    private static void OnMouseWheel(IMouse mouse, ScrollWheel scroll)
    {
        _cameraZoom = Math.Clamp(_cameraZoom - scroll.Y, 1f, 45f);
    }

    private static void OnMouseMove(IMouse mouse, Vector2 pos)
    {
        float lookSense = 0.1f;
        if (_lastMousePos == default) _lastMousePos = pos;
        else
        {
            float xOffset = (pos.X - _lastMousePos.X) * lookSense;
            float yOffset = (pos.Y - _lastMousePos.Y) * lookSense;
            _lastMousePos = pos;

            _cameraYaw += xOffset;
            _cameraPitch -= yOffset;

            _cameraPitch = Math.Clamp(_cameraPitch, -89f, 89f);

            _cameraDir.X = MathF.Cos(MathHelper.DegreesToRadians(_cameraYaw)) *
                           MathF.Cos(MathHelper.DegreesToRadians(_cameraPitch));

            _cameraDir.Y = MathF.Sin(MathHelper.DegreesToRadians(_cameraPitch));

            _cameraDir.Z = MathF.Sin(MathHelper.DegreesToRadians(_cameraYaw)) *
                           MathF.Cos(MathHelper.DegreesToRadians(_cameraPitch));
            _cameraFront = Vector3.Normalize(_cameraDir);
        }
    }

    private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        if (arg2 == Key.Escape)
        {
            _window.Close();
        }
    }

    private static void OnUpdate(double deltaTime)
    {
        float moveSpeed = 2.5f * (float) deltaTime;

        if(_primaryKeyboard is null)
            return;
        
        if (_primaryKeyboard.IsKeyPressed(Key.W))
            _cameraPos += moveSpeed * _cameraFront;
        if (_primaryKeyboard.IsKeyPressed(Key.S))
            _cameraPos -= moveSpeed * _cameraFront;
        if (_primaryKeyboard.IsKeyPressed(Key.A))
            _cameraPos -= moveSpeed * Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp));
        if (_primaryKeyboard.IsKeyPressed(Key.D))
            _cameraPos += moveSpeed * Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp));
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

        var view = Matrix4x4.CreateLookAt(_cameraPos, _cameraPos + _cameraFront, _cameraUp);
        var projection =
            Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(_cameraZoom), Width / Height, 0.1f, 100f);

        _shader.SetUniform("uModel", model);
        _shader.SetUniform("uView", view);
        _shader.SetUniform("uProjection", projection);

        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }

    private static void OnResize(Vector2D<int> size)
    {
        _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
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