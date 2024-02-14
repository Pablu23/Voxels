using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Window = Silk.NET.Windowing.Window;

// ReSharper disable FieldCanBeMadeReadOnly.Local
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Voxels;

public static class Program
{
    private static IWindow _window;
    private static GL _gl;
    private static IKeyboard? _primaryKeyboard;

    private static Texture _texture;
    private static Shader _shader;
    private static Model _model;
    
    private static Camera _camera;

    private static Vector2 _lastMousePos;

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

        _camera = new Camera(new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, -1f), Vector3.UnitY,
            (float)_window.Size.X / (float)_window.Size.Y);
        
        _shader = new Shader(_gl, "shader.vert", "shader.frag");
        _texture = new Texture(_gl, "silk.png");
        _model = new Model(_gl, "cube.model");
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

        _texture.Bind();
        _shader.Use();    
        _shader.SetUniform("uTexture0", 0);

        float difference = (float)(_window.Time * 100);

        Matrix4x4 model = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference)) *
                          Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference));

        Matrix4x4 view = _camera.GetViewMatrix();
        Matrix4x4 projection = _camera.GetProjectionMatrix();

        foreach (Mesh mesh in _model.Meshes)
        {
            mesh.Bind();
            _shader.Use();
            _texture.Bind();
            _shader.SetUniform("uTexture0", 0);
            _shader.SetUniform("uModel", model);
            _shader.SetUniform("uView", view);
            _shader.SetUniform("uProjection", projection);
            
            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)mesh.Vertices.Length);
        }
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
        _model.Dispose();
        _shader.Dispose();
        _texture.Dispose();
    }
}