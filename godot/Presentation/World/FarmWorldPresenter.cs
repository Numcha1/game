using Godot;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
namespace SeasonsTogether;
public partial class FarmWorldPresenter : Node
{
    FarmScene root = null!;
    GameReadModel read = null!;
    AssetCatalog Art = null!;
    TileMapLayer ground = null!, details = null!;
    Node2D actors = null!;
    ShaderMaterial wind = null!;
    int season = -1, quality = 2;
    bool reducedMotion;
    ShaderMaterial water = null!;
    public int GroundCellCount => ground.GetUsedCells().Count;
    public int TreeCount => trees.Count;
    public int CurrentSeason => season;
    readonly Sprite2D[] crops = new Sprite2D[24];
    readonly Sprite2D[] stakes = new Sprite2D[24];
    readonly List<Sprite2D> trees = new();
    readonly List<Sprite2D> treeShadows = new();
    Line2D selection = null!;
    public int SelectedPlot { get; private set; } = -1;
    readonly Vector2[] treeLocations = { new(38,170), new(100,335), new(42,477), new(740,180), new(737,354), new(615,490), new(370,50), new(485,45), new(208,460), new(431,461) };
    public void Initialize(FarmScene owner, GameReadModel model, AssetCatalog art)
    {
        root = owner; read = model; Art = art;
        ground = root.GetNode<TileMapLayer>("Ground"); details = root.GetNode<TileMapLayer>("Details"); actors = root.GetNode<Node2D>("Actors");
        wind = new ShaderMaterial { Shader = Art.WindShader };
        ground.TileSet = Art.TileSet; details.TileSet = ground.TileSet;
        BuildEnvironment(); RefreshSeason(); RefreshPlots();
        selection = new Line2D { Width = 1, DefaultColor = new Color("ffe3a0"), Closed = true, ZIndex = 9, Visible = false,
            Points = new Vector2[] { new(-15, -15), new(15, -15), new(15, 15), new(-15, 15) } };
        root.AddChild(selection);
    }
    public void SelectPlot(int index)
    {
        SelectedPlot = index;
        if (index < 0) { selection.Visible = false; return; }
        var tile = GameCatalog.PlotTile(index);
        selection.Position = new Vector2(tile.X * 32, tile.Y * 32); selection.Visible = true;
    }
    public void SetReducedMotion(bool value) { reducedMotion = value; SetQuality(quality); water.SetShaderParameter("motion", value ? 0f : 1f); }
    public void SetQuality(int quality) { this.quality = quality; wind.SetShaderParameter("strength", quality == 0 || reducedMotion ? 0 : .8f); foreach (var shadow in treeShadows) shadow.Visible = quality > 0; }
    Sprite2D Prop(string asset, Vector2 position, float width, int z = 0)
    {
        var texture = Art.Region(asset);
        var sprite = new Sprite2D { Texture = texture, Position = position, Offset = new Vector2(0, -texture.GetHeight() / 2f), Scale = Vector2.One * (width / texture.GetWidth()), ZIndex = z };
        actors.AddChild(sprite); return sprite;
    }
    void BuildEnvironment()
    {
        Prop("house", new Vector2(112, 128), 120);
        Prop("shop", new Vector2(656, 128), 120);
        Prop("crate", new Vector2(640, 256), 46);
        Prop("well", new Vector2(192, 144), 38);
        Prop("barrel", new Vector2(50, 116), 24);
        Prop("sign", new Vector2(607, 126), 30);
        var pond = new Sprite2D { Texture = Art.Pond, Position = new Vector2(624, 448), ZIndex = -1 };
        water = new ShaderMaterial { Shader = Art.WaterShader }; pond.Material = water; root.AddChild(pond);
        for (int i = 0; i < treeLocations.Length; i++)
        {
            var shadow = new Sprite2D { Texture = Art.Shadow, Position = treeLocations[i] + new Vector2(8, -2), Scale = new Vector2(1.6f, .8f), ZIndex = -2 }; root.AddChild(shadow); treeShadows.Add(shadow);
            var tree = Prop("spring", treeLocations[i], i % 3 == 0 ? 95 : 78); tree.Material = wind; trees.Add(tree);
        }
        var rng = new Random(621);
        for (int i = 0; i < 32; i++)
        {
            var p = new Vector2(rng.Next(30, 760), rng.Next(118, 533));
            if (p.X > 210 && p.X < 475 && p.Y > 160 && p.Y < 347 || p.X > 535 && p.Y > 365) continue;
            Prop(i % 5 == 0 ? "rocks" : "flowers", p, i % 5 == 0 ? 22 : 15);
        }
        for (int i = 0; i < 24; i++)
        {
            var t = GameCatalog.PlotTile(i); var pos = new Vector2(t.X * 32, t.Y * 32);
            crops[i] = new Sprite2D { Position = pos, Visible = false, Material = wind }; actors.AddChild(crops[i]);
            stakes[i] = new Sprite2D { Texture = Art.Stake, Position = pos + new Vector2(9, -10), Visible = false }; actors.AddChild(stakes[i]);
        }
    }
    public void RefreshSeason()
    {
        if (season == read.World.Season) return; season = read.World.Season;
        string[] variants = { "spring", "summer", "autumn", "winter" };
        for (int i = 0; i < trees.Count; i++)
        {
            var texture = Art.Region(variants[season]); float width = i % 3 == 0 ? 95 : 78;
            trees[i].Texture = texture; trees[i].Offset = new Vector2(0, -texture.GetHeight() / 2f); trees[i].Scale = Vector2.One * (width / texture.GetWidth());
        }
        for (int y = 0; y < 19; y++) for (int x = 0; x < 26; x++)
        {
            bool path = y == 4 || y == 10 || x == 6 || x == 18;
            int variation = (x * 31 + y * 17) % 4;
            ground.SetCell(new Vector2I(x, y), 0, new Vector2I(path ? 4 + variation : variation, season));
            if (y == 0 || y == 17) details.SetCell(new Vector2I(x, y), 0, new Vector2I(12, season));
        }
        ground.UpdateInternals();
    }
    public void RefreshPlots()
    {
        for (int i = 0; i < 24; i++)
        {
            var plot = read.Plot(i); var tile = GameCatalog.PlotTile(i);
            ground.SetCell(new Vector2I(tile.X, tile.Y), 0, new Vector2I(plot.Moisture >= 80 ? 10 : plot.Moisture >= 35 ? 9 : 8, season));
            crops[i].Visible = plot.Crop != ""; stakes[i].Visible = plot.Stake;
            if (!crops[i].Visible) continue;
            float progress = (float)Math.Clamp(plot.Growth / GameCatalog.Crops[plot.Crop].Days, 0, 1);
            int stage = progress >= 1 ? 3 : progress < .25f ? 0 : progress < .5f ? 1 : 2;
            crops[i].Texture = Art.CropStage(plot.Crop,stage);
            crops[i].Scale = Vector2.One;
            crops[i].Offset = new Vector2(0,-23);
            crops[i].Modulate = plot.Disease ? new Color(.75f, .65f, .6f) : Colors.White;
        }
    }
}
