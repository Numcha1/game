using Godot;
namespace SeasonsTogether;
/// <summary>Original camera damping and rounding; no input or game state responsibility.</summary>
public sealed class PixelCameraFollower(Camera2D camera)
{
    Vector2 position;
    public void Initialize(Vector2 start)
    {
        camera.LimitLeft = 0; camera.LimitTop = 0; camera.LimitRight = 800; camera.LimitBottom = 576;
        position = start; camera.GlobalPosition = start.Round();
    }
    public void Follow(Vector2 target, double delta)
    {
        position = position.Lerp(target,(float)(1 - Math.Exp(-7 * delta)));
        camera.GlobalPosition = position.Round();
    }
}
