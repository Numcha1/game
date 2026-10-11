using Godot;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;
/// <summary>Bounded scene-owned sprites. Atlas is shared; rebuild occurs only after a furniture command.</summary>
public partial class FurnitureRoomView : Node2D
{
    FarmScene farm=null!;
    int revision=-1;
    readonly Dictionary<string,Node2D> objects=new();
    Sprite2D ghost=null!;
    Polygon2D footprint=null!;
    public int PlacedVisualCount=>objects.Count;
    public void Initialize(FarmScene scene)
    {
        farm=scene;ghost=new Sprite2D {Name="FurniturePreview",Visible=false,ZIndex=20,Offset=new Vector2(0,-42)};AddChild(ghost);
        footprint=new Polygon2D{Name="PlacementFootprint",Visible=false,ZIndex=19};AddChild(footprint);
    }
    public void SetPreview(PlaceableFurnitureDefinition? d,WorldPoint point,int rotation,string error)
    {
        ghost.Visible=footprint.Visible=d!=null;if(d==null)return;
        ghost.Texture=farm.Art.FurnitureFrame(d.Frame(rotation));ghost.Position=new((float)point.X*32,(float)point.Y*32);
        ghost.Modulate=error==""?new Color(.65f,1,.65f,.65f):new Color(1,.4f,.35f,.65f);
        var b=d.Footprint(point.X,point.Y,rotation);var p=new Vector2((float)b.X*32,(float)b.Y*32);var s=new Vector2((float)b.Width*32,(float)b.Height*32);
        footprint.Polygon=new[]{p,p+new Vector2(s.X,0),p+s,p+new Vector2(0,s.Y)};footprint.Color=error==""?new Color(.4f,.9f,.5f,.25f):new Color(1,.3f,.2f,.3f);
    }
    public override void _Process(double delta)
    {
        if(farm==null || farm.Session.ActiveBuildingId!=BuildingRegistry.Farmhouse.Id)return;
        farm.RefreshFurniturePreview();if(revision==farm.Session.FurnitureRevision)return;
        revision=farm.Session.FurnitureRevision;
        foreach(var node in objects.Values){RemoveChild(node);node.QueueFree();}objects.Clear();
        foreach(var p in farm.Session.FurnitureViews()) {
            var def=PlaceableFurnitureCatalog.Find(p.DefinitionId)!;
            var actor=new Node2D{Name="Placed-"+p.Id,Position=new Vector2((float)p.X*32,(float)p.Y*32)};AddChild(actor);objects.Add(p.Id,actor);
            actor.AddChild(new Sprite2D{Name="ContactShadow",Texture=farm.Art.Shadow,Position=new Vector2(0,-2),Scale=new Vector2((float)def.Width*.45f,.22f),Modulate=new Color(1,1,1,.7f),ZIndex=-1});
            actor.AddChild(new Sprite2D{Name="FurnitureSprite",Texture=farm.Art.FurnitureFrame(def.Frame(p.Rotation)),Offset=new Vector2(0,-42)});
            if(def.Interaction==PlacedInteraction.Display) {
                int index=0;
                foreach(var item in p.Contents) {
                    float x,y;
                    if(def.Id=="bookshelf"){x=(index%3-1)*12;y=-51+index/3*24;}
                    else if(p.Rotation==1){x=(index%2-.5f)*10;y=-44+index/2*12;}
                    else{x=(index%2-.5f)*21;y=-30+index/2*10;}
                    var icon=farm.Art.ItemIcon(ItemCatalog.Find(item.Key)!.Icon);float ratio=Math.Min(15f/icon.GetWidth(),16f/icon.GetHeight());
                    actor.AddChild(new Sprite2D{Name="Displayed-"+item.Key.Replace(':','_'),Texture=icon,Position=new Vector2(x,y),Scale=Vector2.One*ratio});index++;
                }
            }
        }
    }
}
