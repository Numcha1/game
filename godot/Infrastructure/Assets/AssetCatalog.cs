using Godot;

namespace SeasonsTogether;

/// <summary>Imported PNG textures and atlas regions are shared by every scene instance.</summary>
public sealed class AssetCatalog
{
    static AssetCatalog? shared;
    public static AssetCatalog Shared => shared ??= new AssetCatalog();
    // Godot resources are initialized and consumed only on the scene thread.
    public AssetCatalog()
    {
        if (Tiles == null || Props == null || Characters == null || TileSet == null || Pond == null || Shadow == null || Stake == null || Glow == null || WindShader == null || WaterShader == null)
            throw new InvalidDataException("Required farm resource missing");
        if (definitions == null || Characters.GetWidth() < 160 || Characters.GetHeight() < 224) throw new InvalidDataException("Invalid farm atlas");
        foreach (var pair in definitions)
        {
            var r = pair.Value;
            if (r == null || r.Length != 4 || r[0] < 0 || r[1] < 0 || r[2] <= 0 || r[3] <= 0 || (long)r[0] + r[2] > Props.GetWidth() || (long)r[1] + r[3] > Props.GetHeight())
                throw new InvalidDataException("Invalid atlas region: " + pair.Key);
        }
        foreach (var crop in Core.Domain.GameCatalog.CropKeys)
            if (!definitions.ContainsKey(crop) || !definitions.ContainsKey("young-" + crop)) throw new InvalidDataException("Missing crop atlas region: " + crop);
        foreach (string key in new[] { "spring", "summer", "autumn", "winter", "sprout", "house", "shop", "crate", "well", "barrel", "sign", "rocks", "flowers", "seeds", "water", "hoe" })
            if (!definitions.ContainsKey(key)) throw new InvalidDataException("Missing prop atlas region: " + key);
    }
    public readonly Texture2D Tiles = GD.Load<Texture2D>("res://Assets/terrain.png");
    public readonly Texture2D Props = GD.Load<Texture2D>("res://Assets/props.png");
    public readonly Texture2D Characters = GD.Load<Texture2D>("res://Assets/characters.png");
    public readonly Texture2D Pond = GD.Load<Texture2D>("res://Assets/pond.png");
    public readonly Texture2D Shadow = GD.Load<Texture2D>("res://Assets/shadow.png");
    public readonly Texture2D Stake = GD.Load<Texture2D>("res://Assets/stake.png");
    public readonly Texture2D Glow = GD.Load<Texture2D>("res://Assets/glow.png");
    public readonly Shader WaterShader = GD.Load<Shader>("res://Shaders/Water.gdshader");
    public readonly Shader WindShader = GD.Load<Shader>("res://Shaders/Wind.gdshader");
    public readonly TileSet TileSet = GD.Load<TileSet>("res://Assets/FarmTiles.tres");
    SpriteFrames? farmerFrames;
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
        if (farmerFrames != null) return farmerFrames;
        var frames = new SpriteFrames(); frames.RemoveAnimation("default");
        string[] directions = { "south", "west", "east", "north" };
        for (int row = 0; row < 4; row++)
        {
            frames.AddAnimation(directions[row]); frames.SetAnimationSpeed(directions[row], 8);
            for (int col = 0; col < 4; col++)
                frames.AddFrame(directions[row], new AtlasTexture { Atlas = Characters, Region = new Rect2(col * 40, row * 56, 40, 56), FilterClip = true });
        }
        farmerFrames = frames; return frames;
    }
}
