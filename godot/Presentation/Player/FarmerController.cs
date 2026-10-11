using Godot;
using SeasonsTogether.Core.Application;

namespace SeasonsTogether;

public partial class FarmerController : Node2D
{
    public GameSession Session { get; set; } = null!;
    public bool InputBlocked { get; set; }
    public AnimatedSprite2D Sprite { get; private set; } = null!;
    public Camera2D Camera { get; private set; } = null!;
    PixelCameraFollower cameraFollower = null!;
    double gesture;
    public override void _Ready()
    {
        Sprite = GetNodeOrNull<Node>("Sprite") as AnimatedSprite2D ?? null!;
        Camera = GetNodeOrNull<Node>("Camera") as Camera2D ?? null!;
        if (Camera != null) cameraFollower = new PixelCameraFollower(Camera);
    }
    public void Initialize(GameSession session, SpriteFrames frames)
    {
        Session = session; Sprite.SpriteFrames = frames; Sprite.Animation = "south";
        Position = new Vector2((float)session.Read.Player.X * 32, (float)session.Read.Player.Y * 32);
        cameraFollower.Initialize(Position);
    }
    static bool Down(Key a, Key b) => Input.IsPhysicalKeyPressed(a) || Input.IsPhysicalKeyPressed(b);
    public override void _PhysicsProcess(double delta)
    {
        if (Session == null || Sprite == null || Camera == null || cameraFollower == null) return;
        var p = Session.Read.Player;
        Vector2 direction = new((Down(Key.D, Key.Right) ? 1 : 0) - (Down(Key.A, Key.Left) ? 1 : 0), (Down(Key.S, Key.Down) ? 1 : 0) - (Down(Key.W, Key.Up) ? 1 : 0));
        if (InputBlocked || gesture > 0 || Session.Read.World.Paused || p.Sleeping) direction = Vector2.Zero;
        direction = direction.Normalized();
        // Match the original four tiles/second speed and axis-separated collision rule.
        double x = p.X + direction.X * 4 * delta, y = p.Y + direction.Y * 4 * delta;
        Session.Move(x, p.Y); Session.Move(Session.Read.Player.X, y);
        p = Session.Read.Player;
        Position = new Vector2((float)p.X * 32, (float)p.Y * 32).Round();
        if (direction != Vector2.Zero)
        {
            Sprite.Animation = Math.Abs(direction.X) > Math.Abs(direction.Y) ? direction.X < 0 ? "west" : "east" : direction.Y < 0 ? "north" : "south";
            Sprite.Play();
        }
        else { Sprite.Stop(); Sprite.Frame = 1; }
        gesture = Math.Max(0, gesture - delta);
        Sprite.Position = new Vector2(0, -22 + (gesture > 0 ? 2 : 0));
        cameraFollower.Follow(Position,delta);
    }
    public void Gesture() => gesture = 0.28;
}
