using System.Text.Json.Serialization;
using SuanPleng;

namespace SeasonsTogether.Core.Domain;

public readonly record struct WorldPoint(double X, double Y)
{
    public double Distance(WorldPoint other) => Math.Sqrt(Math.Pow(X-other.X,2)+Math.Pow(Y-other.Y,2));
}
public readonly record struct BuildingObstacle(double X, double Y, double Width, double Height)
{
    public bool Contains(WorldPoint p) => p.X >= X-.22 && p.X <= X+Width+.22 && p.Y >= Y-.22 && p.Y <= Y+Height+.22;
}
public sealed record BuildingDefinition(string Id, string Name, string Type, string ExteriorRegion,
    WorldPoint Entrance, WorldPoint Return, string Scene, WorldPoint Spawn, WorldPoint Exit,
    WorldPoint Bed, WorldPoint Chest, IReadOnlyList<BuildingObstacle> Obstacles,
    bool OwnerOnly = false, int Opens = 0, int Closes = 1440)
{
    public bool Walkable(WorldPoint p) => double.IsFinite(p.X) && double.IsFinite(p.Y) &&
        p.X >= 1.3 && p.X <= 22.7 && p.Y >= 3.8 && p.Y <= 14.4 && !Obstacles.Any(o=>o.Contains(p));
}
public static class BuildingRegistry
{
    public static readonly BuildingDefinition Farmhouse = new("moonroot.farmhouse", "บ้าน Moonroot", "farmhouse", "moonroot.farm",
        new(3.5,4), new(3.5,4.5), "res://Scenes/Interiors/Farmhouse.tscn", new(12,13.3), new(12,14),
        new(4.5,8), new(19.2,6.1), Array.AsReadOnly(new[]{
            new BuildingObstacle(3,2.5,3.2,4.9), new BuildingObstacle(17.8,2.9,2.7,2.5),
            new BuildingObstacle(1.6,2.8,1.4,2.1),new BuildingObstacle(9.5,2.6,5,2.2),
            new BuildingObstacle(20.7,2.2,1.7,2.4),new BuildingObstacle(1.1,8.5,1,1.7),new BuildingObstacle(21.6,8.5,1,3.2)}));
    public static IReadOnlyList<BuildingDefinition> All { get; } = Array.AsReadOnly(new[]{Farmhouse});
    public static BuildingDefinition? Find(string id) => All.FirstOrDefault(b=>b.Id==id);
}
public sealed class InteriorState
{
    public const int MaximumStorageStack = 1000000;
    [JsonRequired] public int Level { get; set; } = 1;
    [JsonRequired] public Dictionary<string,int> Storage { get; set; } = new();
    [JsonRequired] public bool LightsOn { get; set; } = true;
}
public sealed class InteriorLocation
{
    [JsonRequired] public string BuildingId { get; set; } = "";
    [JsonRequired] public double X { get; set; }
    [JsonRequired] public double Y { get; set; }
}
public sealed class BuildingState
{
    [JsonRequired] public Dictionary<string,InteriorState> Interiors { get; set; } = new();
    [JsonRequired] public Dictionary<string,InteriorLocation> Locations { get; set; } = new();
    public void Validate(Farm farm)
    {
        if(Interiors==null || Locations==null || Interiors.Count>BuildingRegistry.All.Count || Locations.Count>farm.Players.Count)
            throw new InvalidDataException("ข้อมูลอาคารไม่สมบูรณ์");
        foreach(var pair in Interiors)
            if(BuildingRegistry.Find(pair.Key)==null || pair.Value==null || pair.Value.Level!=1 || pair.Value.Storage==null ||
                pair.Value.Storage.Any(i=>!Farm.Crops.ContainsKey(i.Key) || i.Value<=0 || i.Value>InteriorState.MaximumStorageStack))
                throw new InvalidDataException("อาคารหรือของในกล่องไม่รองรับ ไม่มีการทิ้งข้อมูลเดิม");
        foreach(var pair in Locations) {
            var p=pair.Value;
            if(p==null || !farm.Players.Any(player=>player.Id==pair.Key) || p.BuildingId==null || !Interiors.ContainsKey(p.BuildingId) || BuildingRegistry.Find(p.BuildingId) is not { } def || !def.Walkable(new(p.X,p.Y)))
                throw new InvalidDataException("ตำแหน่งภายในอาคารไม่ถูกต้อง");
        }
    }
}
