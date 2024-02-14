using System.Net.Sockets;
using System.Numerics;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;

namespace Voxels;

public class Model : IDisposable
{
    private readonly GL _gl;
    private Assimp _assimp;
    private List<Texture> _texturesLoaded = [];
    public string Directory { get; protected set; } = string.Empty;
    public List<Mesh> Meshes { get; protected set; } = [];
    
    public Model(GL gl, string path, bool gamma = false)
    {
        _gl = gl;
        _assimp = Assimp.GetApi();

        LoadModel(path);
    }

    private unsafe void LoadModel(string path)
    {
        Scene* scene = _assimp.ImportFile(path, (uint)PostProcessSteps.Triangulate);

        if (scene is null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode is null)
        {
            string error = _assimp.GetErrorStringS();
            throw new Exception(error);
        }

        Directory = path;
        ProcessNode(scene->MRootNode, scene);
    }

    private unsafe void ProcessNode(Node* node, Scene* scene)
    {
        for (int i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
            Meshes.Add(ProcessMesh(mesh, scene));
        }

        for (int i = 0; i < node->MNumChildren; i++)
        {
            ProcessNode(node->MChildren[i], scene);
        }
    }

    private unsafe Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene)
    {
        List<Vertex> vertices = [];
        List<uint> indices = [];
        List<Texture> textures = [];

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            Vertex vertex = new()
            {
                BoneIds = new int[Vertex.MaxBoneInfluence],
                Weights = new float[Vertex.MaxBoneInfluence],
                Position = mesh->MVertices[i]
            };

            if (mesh->MNormals is not null)
                vertex.Normal = mesh->MNormals[i];
            
            if (mesh->MTangents is not null)
                vertex.Tangent = mesh->MTangents[i];

            if (mesh->MBitangents is not null)
                vertex.Bitangent = mesh->MBitangents[i];

            if (mesh->MTextureCoords[0] is not null)
            {
                Vector3 texcoords3 = mesh->MTextureCoords[0][i];
                vertex.TexCoords = new Vector2(texcoords3.X, texcoords3.Y);
            }
            
            vertices.Add(vertex);
        }

        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            Face face = mesh->MFaces[i];
            for(uint j = 0; j < face.MNumIndices; j++)
                indices.Add(face.MIndices[j]);
        }

        Material* material = scene->MMaterials[mesh->MMaterialIndex];

        List<Texture> diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse, "texture_diffuse");
        if(diffuseMaps.Count != 0)
            textures.AddRange(diffuseMaps);

        List<Texture> specularMaps = LoadMaterialTextures(material, TextureType.Specular, "texture_specular");
        if(specularMaps.Count != 0)
            textures.AddRange(specularMaps);

        List<Texture> normalMaps = LoadMaterialTextures(material, TextureType.Height, "texture_normal");
        if(normalMaps.Count != 0)
            textures.AddRange(normalMaps);
        
        List<Texture> heightMaps = LoadMaterialTextures(material, TextureType.Ambient, "texture_height");
        if(heightMaps.Count != 0)
            textures.AddRange(normalMaps);

        var result = new Mesh(_gl, BuildVertices(vertices), BuildIndices(indices), textures);
        return result;
    }

    private unsafe List<Texture> LoadMaterialTextures(Material* mat, TextureType type, string typeName)
    {
        var textureCount = _assimp.GetMaterialTextureCount(mat, type);
        List<Texture> textures = [];
        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
            bool skip = false;
            for (int j = 0; j < _texturesLoaded.Count; j++)
            {
                if (_texturesLoaded[j].Path != path) continue;
                
                textures.Add(_texturesLoaded[j]);
                skip = true;
                break;
            }

            if (skip) continue;
            
            var texture = new Texture(_gl, Directory, type);
            texture.Path = path;
            textures.Add(texture);
            _texturesLoaded.Add(texture);
        }

        return textures;
    }
    
    private uint[] BuildIndices(List<uint> indices)
    {
        return indices.ToArray();
    }

    private float[] BuildVertices(List<Vertex> vertexCollection)
    {
        List<float> vertices = [];

        foreach (var vertex in vertexCollection)
        {
            vertices.Add(vertex.Position.X);
            vertices.Add(vertex.Position.Y);
            vertices.Add(vertex.Position.Z);
            vertices.Add(vertex.TexCoords.X);
            vertices.Add(vertex.TexCoords.Y);
        }

        return vertices.ToArray();
    }

    public void Dispose()
    {
        foreach (Mesh mesh in Meshes)
        {
            mesh.Dispose();
        }

        _texturesLoaded = null;
    }
}