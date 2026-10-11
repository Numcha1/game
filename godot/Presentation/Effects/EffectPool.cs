using Godot;

namespace SeasonsTogether;

public partial class EffectPool : Node2D
{
    const int Capacity = 96;
    readonly Sprite2D[] sprites = new Sprite2D[Capacity];
    readonly Vector2[] velocities = new Vector2[Capacity];
    readonly float[] lives = new float[Capacity];
    readonly Random random = new(99);
    int cursor;
    ImageTexture texture = null!;
    public int Quality { get; set; } = 2;
    public bool ReducedMotion { get; set; }
    public override void _Ready()
    {
        using var image = Image.CreateEmpty(3, 3, false, Image.Format.Rgba8); image.Fill(Colors.White);
        texture = ImageTexture.CreateFromImage(image);
        for (int i = 0; i < Capacity; i++) { sprites[i] = new Sprite2D { Texture = texture, Visible = false }; AddChild(sprites[i]); }
    }
    public void Burst(Vector2 position, string kind, Texture2D? pickup = null)
    {
        if (ReducedMotion) return;
        Color color = kind == "water" ? new Color("abdde5") : kind == "harvest" || kind == "sell" ? new Color("f5d37b") : new Color("bb9061");
        int count = Quality == 0 ? 6 : Quality == 1 ? 12 : 20;
        if(kind=="sell") { count=Quality==0?2:Quality==1?4:6; color=Colors.White; }
        for (int n = 0; n < count; n++)
        {
            int i = cursor++ % Capacity; sprites[i].Texture=kind=="sell"?AssetCatalog.Shared.ToolIcon("coin"):n==0 && pickup!=null?pickup:texture;
            sprites[i].Position = position; sprites[i].Modulate = color; sprites[i].Visible = true;
            velocities[i] = new Vector2(random.Next(-42, 43), random.Next(-70, -15)); lives[i] = .6f;
        }
    }
    public override void _Process(double delta)
    {
        for (int i = 0; i < Capacity; i++)
        {
            if(ReducedMotion){lives[i]=0;sprites[i].Visible=false;continue;}
            if (lives[i] <= 0) continue;
            lives[i] -= (float)delta; velocities[i].Y += (float)delta * 110;
            sprites[i].Position += velocities[i] * (float)delta;
            sprites[i].Modulate = new Color(sprites[i].Modulate, Math.Max(0, lives[i] / .6f));
            if (lives[i] <= 0) sprites[i].Visible = false;
        }
    }
    public override void _ExitTree() => texture?.Dispose();
}
