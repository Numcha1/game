using Godot;
using SeasonsTogether.Core.Application;

namespace SeasonsTogether;

public partial class WeatherLighting : Node2D
{
    public GameReadModel Read { get; set; } = null!;
    public bool ReducedMotion { get; set; }
    public int Quality { get; set; } = 2;
    public CanvasModulate Ambient { get; set; } = null!;
    readonly List<PointLight2D> lamps = new();
    readonly Sprite2D[] drops = new Sprite2D[120];
    double elapsed;
    Texture2D rain = null!;
    public override void _Ready()
    {
        using var image = Image.CreateEmpty(2, 9, false, Image.Format.Rgba8);
        image.Fill(new Color(0.72f, 0.88f, 0.91f, 0.6f)); rain = ImageTexture.CreateFromImage(image);
        var random = new Random(702);
        for (int i = 0; i < drops.Length; i++)
        {
            drops[i] = new Sprite2D { Texture = rain, Position = new Vector2(random.Next(800), random.Next(576)), Rotation = -0.18f, Visible = false };
            AddChild(drops[i]);
        }
    }
    public void AddLamp(Vector2 position, Texture2D texture)
    {
        var lamp = new PointLight2D { Position = position, Texture = texture, TextureScale = 1.5f, Color = new Color("ffdba3"), Energy = 0, ShadowEnabled = false, RangeZMax = 8 };
        AddChild(lamp); lamps.Add(lamp);
    }
    public override void _Process(double delta)
    {
        if (Read == null) return;
        elapsed += delta;
        float night = (float)Math.Clamp((Read.World.Minute - 1000) / 240, 0, 1);
        bool wet = Read.World.Weather is "ฝนตก" or "พายุ";
        float minute=(float)Read.World.Minute;
        var tint=minute<480 ? new Color("ffe7c4").Lerp(Colors.White,Mathf.Clamp((minute-360)/120,0,1))
            : minute<960 ? Colors.White
            : minute<1100 ? Colors.White.Lerp(new Color("ffd0a3"),(minute-960)/140)
            : new Color("ffd0a3").Lerp(new Color("8995bb"),Mathf.Clamp((minute-1100)/160,0,1));
        if(Read.World.Season==3 && minute<960)tint*=new Color(.96f,.98f,1);
        if (wet) tint *= new Color(.83f, .89f, .96f);
        Ambient.Color = Ambient.Color.Lerp(tint, (float)(1 - Math.Exp(-delta * 2)));
        foreach (var lamp in lamps) lamp.Energy = Quality == 0 ? 0 : night * 0.65f;
        int count = Quality switch { 0 => 24, 1 => 64, _ => 120 };
        for (int i = 0; i < drops.Length; i++)
        {
            var sprite = drops[i]; sprite.Visible = wet && i < count && !ReducedMotion;
            if (!sprite.Visible) continue;
            sprite.Modulate = new Color(1,1,1,i%3==0?.45f:i%3==1?.7f:1);
            var p = sprite.Position; p.Y += (float)delta * (110 + i % 3 * 60); p.X -= (float)delta * 25;
            if (p.Y > 590) p.Y -= 605; if (p.X < -8) p.X += 816; sprite.Position = p;
        }
    }
    public override void _ExitTree() { rain?.Dispose(); lamps.Clear(); }
}
