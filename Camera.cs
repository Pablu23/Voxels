using System.Numerics;

namespace Voxels;

public class Camera
{
    public Vector3 Position { get; set; }
    public Vector3 Front { get; set; }
    
    public Vector3 Up { get; private set; }
    public float AspectRatio { get; set; }

    public float Yaw { get; set; } = -90f;
    public float Pitch { get; set; }

    private float _zoom = 45f;

    public Camera(Vector3 position, Vector3 front, Vector3 up, float aspectRatio)
    {
        Position = position;
        AspectRatio = aspectRatio;
        Front = front;
        Up = up;
    }

    public void ModifyZoom(float zoomAmount)
    {
        _zoom = Math.Clamp(_zoom - zoomAmount, 1f, 45f);
    }

    public void ModifyDirection(float xOffset, float yOffset)
    {
        Yaw += xOffset;
        Pitch -= yOffset;

        Pitch = Math.Clamp(Pitch, -89f, 89f);
        
        Vector3 cameraDir = Vector3.Zero;
        cameraDir.X = MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
        cameraDir.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
        cameraDir.Z = MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));

        Front = Vector3.Normalize(cameraDir);
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Position + Front, Up);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(_zoom), AspectRatio, 0.1f, 100.0f);
    }
}