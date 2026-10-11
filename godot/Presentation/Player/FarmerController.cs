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
    float presentationScale=1;
    AnimatedSprite2D toolSprite = null!;
    readonly Dictionary<string,SpriteFrames> toolFrames = new();
    string facing = "south";
    public bool ToolVisualVisible => toolSprite.Visible;
    public string Facing => facing;
    public override void _Ready()
    {
        Sprite = GetNodeOrNull<Node>("Sprite") as AnimatedSprite2D ?? null!;
        Camera = GetNodeOrNull<Node>("Camera") as Camera2D ?? null!;
        if (Camera != null) cameraFollower = new PixelCameraFollower(Camera);
    }
    public void Initialize(GameSession session, SpriteFrames frames)
    {
        Session = session; Sprite.SpriteFrames = frames; Sprite.Animation = "south";
        var art = AssetCatalog.Shared;
        foreach (string action in new[] { "plant","water","harvest","treat","fertilize","stake","clear" }) toolFrames[action] = art.ToolFrames(action);
        toolSprite = new AnimatedSprite2D { Position=new Vector2(10,-14),Visible=false };
        AddChild(toolSprite);
        AddChild(new Sprite2D { Texture=art.Shadow,Position=new Vector2(2,4),Scale=new Vector2(.45f,.18f),ZIndex=-2 });
        Position = new Vector2((float)session.Read.Player.X * 32, (float)session.Read.Player.Y * 32);
        cameraFollower.Initialize(Position);
    }
    static bool Down(Key a, Key b) => Input.IsPhysicalKeyPressed(a) || Input.IsPhysicalKeyPressed(b);
    public void ResetCamera(Vector2 position,int width,int height,bool indoor=false) { presentationScale=indoor?PresentationScale.IndoorCharacter:1;Sprite.Scale=Vector2.One*presentationScale;toolSprite.Scale=Vector2.One*presentationScale;Position=position.Round();cameraFollower.Initialize(Position,width,height,indoor); }
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
            facing = Math.Abs(direction.X) > Math.Abs(direction.Y) ? direction.X < 0 ? "west" : "east" : direction.Y < 0 ? "north" : "south";
            Sprite.Animation = facing;
            Sprite.Play();
        }
        else if (gesture > 0) { Sprite.Animation=facing; Sprite.Stop(); Sprite.Frame=1; }
        else { Sprite.Animation="idle-" + facing; Sprite.Play(); }
        gesture = Math.Max(0, gesture - delta);
        int pose = Math.Clamp((int)((.28-gesture)/.07),0,3);
        Sprite.Position = new Vector2(0,-22 + (gesture>0 && pose is 1 or 2 ? 1 : 0))*presentationScale;
        if (toolSprite.SpriteFrames != null) {
            toolSprite.Visible = gesture > 0; toolSprite.Frame = pose;
            toolSprite.FlipH = facing == "west"; toolSprite.ZIndex = facing == "north" ? -1 : 1;
            toolSprite.Position = new Vector2(facing == "west" ? -10 : 10,-14 + (pose is 1 or 2 ? 1 : 0))*presentationScale;
        }
        cameraFollower.Follow(Position,delta);
    }
    public void Gesture(string action = "plant") {
        gesture = .28;
        toolSprite.SpriteFrames = toolFrames.GetValueOrDefault(action);
        toolSprite.Visible = toolSprite.SpriteFrames != null;
    }
}
