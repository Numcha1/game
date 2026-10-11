using Godot;
namespace SeasonsTogether;
/// <summary>Authored silhouettes preserve actor/furniture depth without rectangular floor masking.</summary>
public static class RoomDepth
{
    public static Polygon2D Add(Node2D actors,Texture2D texture,string name,params Vector2[] outline)
    {
        float foot=outline.Max(p=>p.Y);
        var node=new Polygon2D{Name=name,Position=new Vector2(0,foot),Texture=texture,Polygon=outline.Select(p=>p-new Vector2(0,foot)).ToArray(),UV=outline,TextureFilter=CanvasItem.TextureFilterEnum.Nearest};
        actors.AddChild(node);return node;
    }
    public static void ServiceRoom(Node2D actors,Texture2D texture,string id)
    {
        if(id.EndsWith("seed-shop")) {
            Add(actors,texture,"SeedDisplayLeft",new(38,102),new(80,58),new(208,58),new(310,108),new(338,154),new(312,203),new(48,203));
            Add(actors,texture,"SeedDisplayRight",new(482,80),new(674,80),new(714,132),new(712,205),new(482,205));
        } else if(id.EndsWith("workshop")) {
            Add(actors,texture,"WorkbenchLeft",new(80,110),new(280,108),new(338,139),new(338,205),new(88,205));
            Add(actors,texture,"WorkbenchRight",new(532,101),new(710,102),new(730,164),new(711,205),new(536,205));
        } else {
            Add(actors,texture,"BedCluster",new(72,81),new(210,65),new(239,125),new(225,225),new(82,225));
            if(id.EndsWith("mira-home"))Add(actors,texture,"ReadingChair",new(118,274),new(214,264),new(243,345),new(206,383),new(133,367));
            else Add(actors,texture,"ReadingChair",new(675,99),new(711,103),new(723,131),new(715,170),new(696,192),new(659,189),new(644,161),new(653,131));
        }
    }
}
