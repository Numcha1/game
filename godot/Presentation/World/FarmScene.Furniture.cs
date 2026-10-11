using Godot;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;
public partial class FarmScene
{
    FurnitureRoomView? furnitureRoom;
    string placingItem="",movingFurniture="";
    int placingRotation;
    WorldPoint placingPoint;
    string placementError="";
    int previewRevision=-1,previewRotation=-1;
    WorldPoint previewPlayer=new(double.NaN,double.NaN),previewPoint=new(double.NaN,double.NaN);
    public bool FurniturePlacementActive=>placingItem!="";
    public string FurniturePlacementPrompt=>"คลิกเพื่อวาง"+(PlaceableFurnitureCatalog.Find(placingItem)?.Rotatable==true?" · R หมุน":"")+" · Esc ยกเลิก";
    internal void AttachFurnitureRoom(Node2D actors)
    {
        furnitureRoom=new FurnitureRoomView{Name="FurnitureObjects",YSortEnabled=true};actors.AddChild(furnitureRoom);furnitureRoom.Initialize(this);
    }
    public void BeginFurniturePlacement(string item,string moving="")
    {
        if(!ReadyToPlay || IsBusy || Session.ActiveBuildingId!=BuildingRegistry.Farmhouse.Id || PlaceableFurnitureCatalog.Find(item) is not {} def){Notify("เดินเข้าบ้านก่อนวางเฟอร์นิเจอร์");return;}
        if(moving=="" && Session.CarriedCount(def.ItemId)<1){Notify("ไม่มีเฟอร์นิเจอร์ในกระเป๋า");return;}
        if(Session.SeatedFurnitureId!=""){Notify("กด F เพื่อยืนขึ้นก่อนวางเฟอร์นิเจอร์");return;}
        placingItem=def.ItemId;movingFurniture=moving;placingRotation=moving==""?0:Session.FurnitureViews().FirstOrDefault(p=>p.Id==moving)?.Rotation??0;
        previewRevision=-1;
        Farmer.InputBlocked=false;UpdateFurniturePreview(GetGlobalMousePosition());Notify("เลือกพื้นที่ · "+FurniturePlacementPrompt);
    }
    public void CancelFurniturePlacement()
    {
        placingItem="";movingFurniture="";
        if(furnitureRoom!=null && GodotObject.IsInstanceValid(furnitureRoom))furnitureRoom.SetPreview(null,default,0,"");
    }
    void UpdateFurniturePreview(Vector2 world)
    {
        placingPoint=new(Math.Round(world.X/16)/2,Math.Round(world.Y/16)/2);
        if(previewPoint==placingPoint && previewPlayer==Session.Position && previewRotation==placingRotation && previewRevision==Session.FurnitureRevision)return;
        previewPoint=placingPoint;previewPlayer=Session.Position;previewRotation=placingRotation;previewRevision=Session.FurnitureRevision;
        placementError=Session.PreviewFurniture(placingItem,placingPoint.X,placingPoint.Y,placingRotation,movingFurniture);
        if(furnitureRoom!=null && GodotObject.IsInstanceValid(furnitureRoom))furnitureRoom.SetPreview(PlaceableFurnitureCatalog.Find(placingItem),placingPoint,placingRotation,placementError);
    }
    internal void RefreshFurniturePreview(){if(FurniturePlacementActive)UpdateFurniturePreview(new((float)placingPoint.X*32,(float)placingPoint.Y*32));}
    public bool HandleFurnitureInput(InputEvent input)
    {
        if(!FurniturePlacementActive)return false;
        if(!ReadyToPlay || IsBusy || TitleActive){CancelFurniturePlacement();return true;}
        if(Hud.ModalVisible)return false;
        if(input is InputEventMouseMotion motion)UpdateFurniturePreview(GetViewport().GetCanvasTransform().AffineInverse()*motion.Position);
        if(input is InputEventKey {Pressed:true,Echo:false} key) {
            if(key.PhysicalKeycode==Key.Escape){CancelFurniturePlacement();Notify("ยกเลิกการวางแล้ว");return true;}
            if(key.PhysicalKeycode==Key.R && PlaceableFurnitureCatalog.Find(placingItem)!.Rotatable){placingRotation=1-placingRotation;UpdateFurniturePreview(new((float)placingPoint.X*32,(float)placingPoint.Y*32));return true;}
            if(key.PhysicalKeycode is Key.B or Key.I){CancelFurniturePlacement();return false;}
        }
        if(input is InputEventMouseButton {Pressed:true,ButtonIndex:MouseButton.Left} click) {
            UpdateFurniturePreview(GetViewport().GetCanvasTransform().AffineInverse()*click.Position);
            var result=Session.PlaceFurniture(placingItem,placingPoint.X,placingPoint.Y,placingRotation,Guid.NewGuid(),movingFurniture);
            Notify(result.Message);if(result.Changed)CancelFurniturePlacement();return true;
        }
        // Movement still works during preview; other game actions wait until placement ends.
        return input is InputEventKey;
    }
}
