using Godot;
using SeasonsTogether.Core;

namespace SeasonsTogether;

public partial class FarmerController : Node2D
{
    public LocalFarmSession Session { get; set; } = null!;
    public bool InputBlocked { get; set; }
    public AnimatedSprite2D Sprite { get; private set; } = null!;
    public Camera2D Camera { get; private set; } = null!;
    Vector2 cameraPosition;
    double gesture;
    public override void _Ready()
    {
        Sprite = GetNode<AnimatedSprite2D>("Sprite"); Camera = GetNode<Camera2D>("Camera");
        Camera.LimitLeft = 0; Camera.LimitTop = 0; Camera.LimitRight = 800; Camera.LimitBottom = 576;
    }
    public void Initialize(LocalFarmSession session, SpriteFrames frames)
    {
        Session = session; Sprite.SpriteFrames = frames; Sprite.Animation = "south";
        Position = new Vector2((float)session.Player.X * 32, (float)session.Player.Y * 32);
        cameraPosition = Position; Camera.GlobalPosition = Position.Round();
    }
    static bool Down(Key a, Key b) => Input.IsPhysicalKeyPressed(a) || Input.IsPhysicalKeyPressed(b);
    public override void _PhysicsProcess(double delta)
    {
        if (Session == null) return;
        var p = Session.Player;
        Vector2 direction = new((Down(Key.D, Key.Right) ? 1 : 0) - (Down(Key.A, Key.Left) ? 1 : 0), (Down(Key.S, Key.Down) ? 1 : 0) - (Down(Key.W, Key.Up) ? 1 : 0));
        if (InputBlocked || gesture > 0 || Session.State.Paused || p.Sleeping) direction = Vector2.Zero;
        direction = direction.Normalized();
        // Match the original four tiles/second speed and axis-separated collision rule.
        double x = p.X + direction.X * 4 * delta, y = p.Y + direction.Y * 4 * delta;
        Session.Move(x, p.Y); Session.Move(p.X, y);
        Position = new Vector2((float)p.X * 32, (float)p.Y * 32).Round();
        if (direction != Vector2.Zero)
        {
            Sprite.Animation = Math.Abs(direction.X) > Math.Abs(direction.Y) ? direction.X < 0 ? "west" : "east" : direction.Y < 0 ? "north" : "south";
            Sprite.Play();
        }
        else { Sprite.Stop(); Sprite.Frame = 1; }
        gesture = Math.Max(0, gesture - delta);
        Sprite.Position = new Vector2(0, -22 + (gesture > 0 ? 2 : 0));
        cameraPosition = cameraPosition.Lerp(Position, (float)(1 - Math.Exp(-7 * delta)));
        Camera.GlobalPosition = cameraPosition.Round();
    }
    public void Gesture() => gesture = 0.28;
}
