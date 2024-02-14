using Silk.NET.OpenGL;

namespace Voxels;

public class Mesh : IDisposable
{
    public GL Gl { get; }
    public float[] Vertices { get; private set; }
    public uint[] Indices { get; private set; }
    public IReadOnlyList<Texture> Textures { get; private set; }
    public VertexArrayObject<float, uint> Vao { get; set; } = null!;
    public BufferObject<float> Vbo { get; set; } = null!;
    public BufferObject<uint> Ebo { get; set; } = null!;

    public Mesh(GL gl, float[] vertices, uint[] indices, IReadOnlyList<Texture> textures)
    {
        Gl = gl;
        Vertices = vertices;
        Indices = indices;
        Textures = textures;
        SetupMesh();
    }

    public void SetupMesh()
    {
        Ebo = new BufferObject<uint>(Gl, Indices, BufferTargetARB.ElementArrayBuffer);
        Vbo = new BufferObject<float>(Gl, Vertices, BufferTargetARB.ArrayBuffer);
        Vao = new VertexArrayObject<float, uint>(Gl, Vbo, Ebo);
        Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        Vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
    }

    public void Bind()
    {
        Vao.Bind();
    }

    public void Dispose()
    {
        Textures = null!;
        Vao.Dispose();
        Vbo.Dispose();
        Ebo.Dispose();
    }
}