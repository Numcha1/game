using Godot;

namespace SeasonsTogether;

/// <summary>Imported PNG textures and atlas regions are shared by every scene instance.</summary>
public sealed class AssetCatalog
{
    public readonly Texture2D Tiles = GD.Load<Texture2D>("res://Assets/terrain.png");
    public readonly Texture2D Props = GD.Load<Texture2D>("res://Assets/props.png");
    public readonly Texture2D Characters = GD.Load<Texture2D>("res://Assets/characters.png");
    readonly Dictionary<string, AtlasTexture> regions = new();
    readonly Dictionary<string, int[]> definitions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int[]>>(Godot.FileAccess.GetFileAsString("res://Assets/regions.json"))!;
    public AtlasTexture Region(string name)
    {
        if (regions.TryGetValue(name, out var texture)) return texture;
        var r = definitions[name];
        texture = new AtlasTexture { Atlas = Props, Region = new Rect2(r[0], r[1], r[2], r[3]), FilterClip = true };
        regions[name] = texture; return texture;
    }
    public SpriteFrames FarmerFrames()
    {
        var frames = new SpriteFrames(); frames.RemoveAnimation("default");
        string[] directions = { "south", "west", "east", "north" };
        for (int row = 0; row < 4; row++)
        {
            frames.AddAnimation(directions[row]); frames.SetAnimationSpeed(directions[row], 8);
            for (int col = 0; col < 4; col++)
                frames.AddFrame(directions[row], new AtlasTexture { Atlas = Characters, Region = new Rect2(col * 40, row * 56, 40, 56), FilterClip = true });
        }
        return frames;
    }
    public TileSet MakeTileSet()
    {
        var atlas = new TileSetAtlasSource { Texture = Tiles, TextureRegionSize = new Vector2I(32, 32) };
        for (int row = 0; row < 4; row++) for (int col = 0; col < 16; col++) atlas.CreateTile(new Vector2I(col, row));
        var set = new TileSet { TileSize = new Vector2I(32, 32) }; set.AddSource(atlas, 0); return set;
    }
}
