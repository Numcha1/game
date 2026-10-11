using Godot;
namespace SeasonsTogether;
/// <summary>Original camera damping and rounding; no input or game state responsibility.</summary>
public sealed class PixelCameraFollower(Camera2D camera)
{
    Vector2 position;
    public void Initialize(Vector2 start,int width=800,int height=576)
    {
        camera.LimitLeft = width<800 ? -16 : 0; camera.LimitTop = 0; camera.LimitRight = width<800 ? width+16 : width; camera.LimitBottom = height;
        position = start; camera.GlobalPosition = start.Round();
    }
    public void Follow(Vector2 target, double delta)
    {
        position = position.Lerp(target,(float)(1 - Math.Exp(-7 * delta)));
        camera.GlobalPosition = position.Round();
    }
}
