using Godot;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;

public partial class VillageRoom : InteriorRoom
{
    FarmScene owner=null!;BuildingDefinition building=null!;
    readonly List<Sprite2D> lights=new();
    public override void Initialize(GameReadModel model,BuildingDefinition def,FarmScene scene)
    {
        owner=scene;building=def;
        if(GetNodeOrNull<Node2D>("Actors") is not {YSortEnabled:true})throw new InvalidDataException("Village scene requires Y sorted Actors");
        Texture2D texture=def.Type=="village"?NpcAssets.Village:NpcAssets.Room(def.Id);
        AddChild(new Sprite2D {Name="Background",Texture=texture,Centered=false,ZIndex=-10,Scale=new Vector2(768f/texture.GetWidth(),512f/texture.GetHeight())});
        foreach(var npc in NpcRegistry.Active) {
            var actor=NpcAssets.CreateActor();GetNode<Node2D>("Actors").AddChild(actor);actor.Initialize(scene.Session,npc.Id,def.Id);
        }
        if(def.Type=="village") {
            foreach(var door in BuildingRegistry.All.Where(b=>b.ParentId==def.Id)) {
                var label=new Label {Text=door.Name,Theme=FarmTypography.Shared,Position=new Vector2((float)door.Entrance.X*32-70,163),Size=new Vector2(140,22),HorizontalAlignment=HorizontalAlignment.Center};
                label.AddThemeFontSizeOverride("font_size",12);label.AddThemeColorOverride("font_color",new Color("fff0c6"));label.AddThemeColorOverride("font_shadow_color",new Color("29382b"));label.AddThemeConstantOverride("shadow_offset_x",1);label.AddThemeConstantOverride("shadow_offset_y",1);AddChild(label);
            }
        } else {
            var label=new Label {Text=def.Name,Theme=FarmTypography.Shared,Position=new Vector2(260,104),Size=new Vector2(248,25),HorizontalAlignment=HorizontalAlignment.Center};label.AddThemeFontSizeOverride("font_size",15);AddChild(label);
            foreach(var pos in new[]{new Vector2(322,72),new Vector2(442,72)}) {
                var light=new Sprite2D{Texture=AssetCatalog.Shared.Glow,Position=pos,Modulate=new Color(1,.75f,.4f,.22f),Scale=Vector2.One*.8f};AddChild(light);lights.Add(light);
            }
        }
    }
    public override void _Process(double delta)
    {
        if(owner==null || !Visible || building.Type=="village")return;
        var world=owner.Session.Read.World;
        float night=Math.Clamp(((float)world.Minute-1020)/180f,0,1);
        owner.GetNode<CanvasModulate>("Ambient").Color=Colors.White.Lerp(new Color(.8f,.78f,.69f),night);
        foreach(var light in lights)light.Visible=world.Minute>=1020;
    }
}
