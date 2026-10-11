using Godot;
namespace SeasonsTogether;
/// <summary>Original camera damping and rounding; no input or game state responsibility.</summary>
public sealed class PixelCameraFollower(Camera2D camera)
{
    Vector2 position;
    bool indoor;int roomWidth,roomHeight;
    public void Initialize(Vector2 start,int width=800,int height=576,bool indoorProfile=false)
    {
        indoor=indoorProfile;roomWidth=width;roomHeight=height;UpdateZoom();
        camera.LimitLeft = width<800 ? -16 : 0; camera.LimitTop = 0; camera.LimitRight = width<800 ? width+16 : width; camera.LimitBottom = height;
        position = start; camera.GlobalPosition = start.Round();camera.ResetSmoothing();camera.ForceUpdateScroll();
    }
    void UpdateZoom()
    {
        var viewport=camera.GetViewportRect().Size;
        // Integer window stretching can expose a larger logical viewport. Cover the room
        // at those sizes instead of revealing pixels outside its background.
        float zoom=indoor?Math.Max(PresentationScale.IndoorZoom,Math.Max(viewport.X/(roomWidth<800?roomWidth+32:roomWidth),viewport.Y/roomHeight)):1;
        camera.Zoom=Vector2.One*zoom;
    }
    public void Follow(Vector2 target, double delta)
    {
        UpdateZoom();
        position = position.Lerp(target,(float)(1 - Math.Exp(-7 * delta)));
        camera.GlobalPosition = position.Round();
    }
}
