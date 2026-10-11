using System.Text.Json.Serialization;
using SuanPleng;

namespace SeasonsTogether.Core.Domain;

public enum PlacedInteraction { Storage, Display, Seat, Craft }
public sealed record PlaceableFurnitureDefinition(string Id, string Name, PlacedInteraction Interaction,
    double Width, double Depth, int AtlasIndex, int Price, int WoodCost, int Capacity, bool Rotatable=false)
{
    public string ItemId => "furniture:"+Id;
    public BuildingObstacle Footprint(double x,double y,int rotation) =>
        new(x-(rotation==1 && Interaction==PlacedInteraction.Display && Id=="wooden-table"?Depth:Width)/2,
            y-(rotation==1 && Id=="wooden-table"?Width:Depth),
            rotation==1 && Id=="wooden-table"?Depth:Width,rotation==1 && Id=="wooden-table"?Width:Depth);
    public int Frame(int rotation) => rotation==1 ? Id=="wooden-table"?6:Id=="wooden-chair"?7:AtlasIndex : AtlasIndex;
}
public static class PlaceableFurnitureCatalog
{
    public const string WoodId="material:wood";
    public static IReadOnlyList<PlaceableFurnitureDefinition> All {get;}=Array.AsReadOnly(new[]{
        new PlaceableFurnitureDefinition("wooden-table","โต๊ะไม้",PlacedInteraction.Display,2,1,0,80,12,4,true),
        new PlaceableFurnitureDefinition("wooden-chair","เก้าอี้ไม้",PlacedInteraction.Seat,1,1,1,35,6,0,true),
        new PlaceableFurnitureDefinition("wooden-box","กล่องไม้",PlacedInteraction.Storage,1.25,1,2,50,8,12),
        new PlaceableFurnitureDefinition("storage-chest","หีบเก็บของ",PlacedInteraction.Storage,1.5,1,3,100,16,24),
        new PlaceableFurnitureDefinition("bookshelf","ชั้นหนังสือ",PlacedInteraction.Display,1.5,1,4,90,14,6),
        new PlaceableFurnitureDefinition("crafting-workbench","โต๊ะงานช่าง",PlacedInteraction.Craft,2,1,5,140,20,0)
    });
    static readonly IReadOnlyDictionary<string,PlaceableFurnitureDefinition> Lookup=All.SelectMany(d=>new[]{(d.Id,d),(d.ItemId,d)}).ToDictionary(p=>p.Item1,p=>p.d,StringComparer.Ordinal);
    public static PlaceableFurnitureDefinition? Find(string? id)=>id!=null && Lookup.TryGetValue(id,out var d)?d:null;
}
public sealed class PlacedFurniture
{
    [JsonRequired] public string Id {get;set;}="";
    [JsonRequired] public string DefinitionId {get;set;}="";
    [JsonRequired] public string OwnerId {get;set;}="";
    [JsonRequired] public string BuildingId {get;set;}="";
    [JsonRequired] public double X {get;set;}
    [JsonRequired] public double Y {get;set;}
    [JsonRequired] public int Rotation {get;set;}
    [JsonRequired] public Dictionary<string,int> Contents {get;set;}=new();
    [JsonIgnore] public BuildingObstacle Bounds=>PlaceableFurnitureCatalog.Find(DefinitionId)!.Footprint(X,Y,Rotation);
}
public sealed class FurnitureState
{
    public const int CarryLimit=999, PlacementLimit=64;
    [JsonRequired] public int Schema {get;set;}=1;
    [JsonRequired] public Dictionary<string,Dictionary<string,int>> Carried {get;set;}=new();
    [JsonRequired] public Dictionary<string,PlacedFurniture> Placed {get;set;}=new();
    [JsonRequired] public List<Guid> AppliedCommands {get;set;}=new();
    public void Validate(Farm farm,BuildingState buildings)
    {
        if(Schema!=1 || Carried==null || Placed==null || AppliedCommands==null || Placed.Count>PlacementLimit || Carried.Count>farm.Players.Count || AppliedCommands.Count>256 || AppliedCommands.Any(g=>g==Guid.Empty) || AppliedCommands.Distinct().Count()!=AppliedCommands.Count)
            throw new InvalidDataException("Invalid furniture schema or command ledger");
        bool Owner(string id)=>farm.Players.Any(p=>p.Id==id);
        foreach(var (owner,bag) in Carried)
            if(!Owner(owner) || bag==null || bag.Count>7 || bag.Any(i=>(i.Key!=PlaceableFurnitureCatalog.WoodId && PlaceableFurnitureCatalog.Find(i.Key)?.ItemId!=i.Key) || i.Value<=0 || i.Value>CarryLimit))
                throw new InvalidDataException("Invalid carried furniture or material");
        foreach(var (id,p) in Placed) {
            var d=p==null?null:PlaceableFurnitureCatalog.Find(p.DefinitionId);
            if(p==null || d==null || p.DefinitionId!=d.Id || id!=p.Id || !Guid.TryParseExact(id,"N",out _) || !Owner(p.OwnerId) || p.BuildingId!=BuildingRegistry.Farmhouse.Id || !buildings.Interiors.ContainsKey(p.BuildingId) || p.Contents==null || p.Rotation<0 || p.Rotation>(d.Rotatable?1:0) || p.Contents.Count>d.Capacity || p.Contents.Any(i=>!Storable(i.Key) || i.Value<=0 || i.Value>(d.Interaction==PlacedInteraction.Display?1:StackLimit(i.Key))))
                throw new InvalidDataException("Invalid placed furniture or contents");
        }
        int index=0;
        foreach(var p in Placed.Values) {
            string error=FurniturePlacement.Error(p,Placed.Values,buildings.Locations.Values,index++==0);
            if(error!="")throw new InvalidDataException(error);
        }
    }
    public static bool Storable(string id)=>ItemCatalog.Find(id) is {} item && (item.Category is ItemCategory.Produce or ItemCategory.Seed || id==PlaceableFurnitureCatalog.WoodId || item.Placeable);
    public static int StackLimit(string id)=>ItemCatalog.Find(id)?.Category==ItemCategory.Produce?InteriorState.MaximumStorageStack:ItemCatalog.Find(id)?.MaxStack??0;
}
public static class FurniturePlacement
{
    static readonly (int,int)[] Steps={(1,0),(-1,0),(0,1),(0,-1)};
    public static bool Overlaps(BuildingObstacle a,BuildingObstacle b,double gap=.25)=>
        a.X<b.X+b.Width+gap && a.X+a.Width+gap>b.X && a.Y<b.Y+b.Height+gap && a.Y+a.Height+gap>b.Y;
    public static bool Walkable(WorldPoint point,IEnumerable<PlacedFurniture> placed)=>
        BuildingRegistry.Farmhouse.Walkable(point) && !placed.Any(p=>p.Bounds.Contains(point));
    public static string Error(PlacedFurniture candidate,IEnumerable<PlacedFurniture> objects,IEnumerable<InteriorLocation> players,bool checkRoutes=true)
    {
        var d=PlaceableFurnitureCatalog.Find(candidate.DefinitionId);
        if(d==null || candidate.BuildingId!=BuildingRegistry.Farmhouse.Id || candidate.Rotation<0 || candidate.Rotation>(d.Rotatable?1:0) || !double.IsFinite(candidate.X) || !double.IsFinite(candidate.Y) || candidate.X*2!=Math.Round(candidate.X*2) || candidate.Y*2!=Math.Round(candidate.Y*2))return "ตำแหน่งหรือทิศทางไม่ถูกต้อง";
        var room=BuildingRegistry.Farmhouse;var b=candidate.Bounds;
        if(b.X<1.55 || b.X+b.Width>22.45 || b.Y<3.8 || b.Y+b.Height>14.15 || room.Obstacles.Any(o=>Overlaps(o,b)))return "พื้นที่ติดผนังหรือเฟอร์นิเจอร์เดิม";
        var others=objects.Where(p=>p.Id!=candidate.Id).ToArray();
        if(others.Any(p=>Overlaps(p.Bounds,b)))return "พื้นที่มีเฟอร์นิเจอร์อยู่แล้ว";
        var targets=new[]{room.Spawn,room.Exit,room.Bed,room.Chest};
        if(targets.Any(p=>b.Contains(p)) || Overlaps(b,new(10.5,12,3,2.4),0))return "ต้องเว้นทางเข้าออกและจุดใช้งานเดิม";
        if(new[]{room.Bed,room.Chest}.Any(p=>Overlaps(b,new(p.X-1,p.Y-1,2,2),0)))return "ต้องเว้นพื้นที่ใช้งานเตียงและหีบเดิม";
        if(players.Any(p=>p.BuildingId==room.Id && b.Contains(new(p.X,p.Y))))return "มีผู้เล่นยืนอยู่ในพื้นที่";
        if(!checkRoutes)return "";
        // A bounded half-tile flood prevents barriers that leave the exit reachable but seal off bed/chest.
        var bounds=others.Append(candidate).Select(p=>p.Bounds).ToArray();var seen=new HashSet<(int,int)>();var queue=new Queue<(int,int)>();
        var start=((int)Math.Round(room.Spawn.X*2),(int)Math.Round(room.Spawn.Y*2));seen.Add(start);queue.Enqueue(start);
        while(queue.TryDequeue(out var cell))foreach(var step in Steps) {
            var next=(cell.Item1+step.Item1,cell.Item2+step.Item2);
            var point=new WorldPoint(next.Item1/2d,next.Item2/2d);
            if(next.Item1<3 || next.Item1>45 || next.Item2<8 || next.Item2>28 || seen.Contains(next) || !room.Walkable(point) || bounds.Any(b=>b.Contains(point)))continue;
            seen.Add(next);queue.Enqueue(next);
        }
        foreach(var p in targets.Concat(players.Where(p=>p.BuildingId==room.Id).Select(p=>new WorldPoint(p.X,p.Y))))
            if(!seen.Any(c=>new WorldPoint(c.Item1/2d,c.Item2/2d).Distance(p)<=.65))return "เฟอร์นิเจอร์ปิดกั้นเส้นทางในบ้าน";
        return "";
    }
}
