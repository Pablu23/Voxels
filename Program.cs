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
    private static VertexArrayObject<float, uint> _vaoCube;
    private static Shader _lightingShader;
    private static Shader _lampShader;

    private static Camera _camera;

    private static Vector2 _lastMousePos;

    private static readonly float[] Vertices =
    {
        //X    Y      Z
        -0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, 0.5f, -0.5f,
        0.5f, 0.5f, -0.5f,
        -0.5f, 0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,

        -0.5f, -0.5f, 0.5f,
        0.5f, -0.5f, 0.5f,
        0.5f, 0.5f, 0.5f,
        0.5f, 0.5f, 0.5f,
        -0.5f, 0.5f, 0.5f,
        -0.5f, -0.5f, 0.5f,

        -0.5f, 0.5f, 0.5f,
        -0.5f, 0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,
        -0.5f, -0.5f, 0.5f,
        -0.5f, 0.5f, 0.5f,

        0.5f, 0.5f, 0.5f,
        0.5f, 0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, 0.5f,
        0.5f, 0.5f, 0.5f,

        -0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, 0.5f,
        0.5f, -0.5f, 0.5f,
        -0.5f, -0.5f, 0.5f,
        -0.5f, -0.5f, -0.5f,

        -0.5f, 0.5f, -0.5f,
        0.5f, 0.5f, -0.5f,
        0.5f, 0.5f, 0.5f,
        0.5f, 0.5f, 0.5f,
        -0.5f, 0.5f, 0.5f,
        -0.5f, 0.5f, -0.5f
    };


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
        _vaoCube = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vaoCube.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);

        _lightingShader = new Shader(_gl, "shader.vert", "lighting.frag");
        _lampShader = new Shader(_gl, "shader.vert", "shader.frag");

        _camera = new Camera(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, Width / Height);
    }

    private static void OnUpdate(double deltaTime)
    {
        float moveSpeed = 2.5f * (float)deltaTime;

        if (_primaryKeyboard is null)
            return;

        if (_primaryKeyboard.IsKeyPressed(Key.W))
            _camera.Position += moveSpeed * _camera.Front;
        if (_primaryKeyboard.IsKeyPressed(Key.S))
            _camera.Position -= moveSpeed * _camera.Front;
        if (_primaryKeyboard.IsKeyPressed(Key.A))
            _camera.Position -= moveSpeed * Vector3.Normalize(Vector3.Cross(_camera.Front, _camera.Up));
        if (_primaryKeyboard.IsKeyPressed(Key.D))
            _camera.Position += moveSpeed * Vector3.Normalize(Vector3.Cross(_camera.Front, _camera.Up));
    }

    private static void OnRender(double obj)
    {
        _gl.Enable(EnableCap.DepthTest);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _vaoCube.Bind();
        _lightingShader.Use();

        _lightingShader.SetUniform("uModel", Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(25f)));
        _lightingShader.SetUniform("uView", _camera.GetViewMatrix());
        _lightingShader.SetUniform("uProjection", _camera.GetProjectionMatrix());
        _lightingShader.SetUniform("objectColor", new Vector3(1f, 0.5f, 0.31f));
        _lightingShader.SetUniform("lightColor", Vector3.One);
        
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

        _lampShader.Use();

        Matrix4x4 lampMatrix = Matrix4x4.Identity;
        lampMatrix *= Matrix4x4.CreateScale(0.2f);
        lampMatrix *= Matrix4x4.CreateTranslation(new Vector3(1.2f, 1.0f, 2.0f));
        
        _lampShader.SetUniform("uModel", lampMatrix);
        _lampShader.SetUniform("uView", _camera.GetViewMatrix());
        _lampShader.SetUniform("uProjection", _camera.GetProjectionMatrix());
    }

    private static void OnMouseWheel(IMouse mouse, ScrollWheel scroll)
    {
        _camera.ModifyZoom(scroll.Y);
    }

    private static void OnMouseMove(IMouse mouse, Vector2 pos)
    {
        const float lookSense = 0.1f;
        if (_lastMousePos == default) _lastMousePos = pos;
        else
        {
            float xOffset = (pos.X - _lastMousePos.X) * lookSense;
            float yOffset = (pos.Y - _lastMousePos.Y) * lookSense;
            _lastMousePos = pos;

            _camera.ModifyDirection(xOffset, yOffset);
        }
    }

    private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        if (arg2 == Key.Escape)
        {
            _window.Close();
        }
    }


    private static void OnResize(Vector2D<int> size)
    {
        _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
    }

    private static void OnClose()
    {
        _vbo.Dispose();
        _ebo.Dispose();
        _vaoCube.Dispose();
        _lightingShader.Dispose();
        _lampShader.Dispose();
    }
}