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

    private static BufferObject<float> _vbo;
    private static BufferObject<uint> _ebo;
    private static VertexArrayObject<float, uint> _vao;
    
    public static Texture Texture;
    private static Shader _shader;

    private static Transform[] Transforms = new Transform[4];
    
    private static readonly float[] Vertices =
    {
        //X    Y      Z     S    T
        0.5f,  0.5f, 0.0f, 1.0f, 0.0f,
        0.5f, -0.5f, 0.0f, 1.0f, 1.0f,
        -0.5f, -0.5f, 0.0f, 0.0f, 1.0f,
        -0.5f,  0.5f, 0.5f, 0.0f, 0.0f
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
        Texture = new Texture(_gl, "silk.png");

        Transforms[0] = new Transform
        {
            Position = new Vector3(0.5f, 0.5f, 0f)
        };

        Transforms[1] = new Transform
        {
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1f)
        };
        //Scaling.
        Transforms[2] = new Transform
        {
            Scale = 0.5f
        };
        //Mixed transformation.
        Transforms[3] = new Transform
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
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        
        _vao.Bind();
        _shader.Use();
        
        Texture.Bind();
        
        _shader.SetUniform("uTexture", 0);

        for (int i = 0; i < Transforms.Length; i++)
        {
            _shader.SetUniform("uModel", Transforms[i].ViewMatrix);
            
            _gl.DrawElements(PrimitiveType.Triangles, (uint) Indices.Length, DrawElementsType.UnsignedInt, null);
            
        }
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
        Texture.Dispose();
    }
}