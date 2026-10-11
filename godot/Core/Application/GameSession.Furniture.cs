using System.Collections.ObjectModel;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether.Core.Application;

public sealed record FurnitureView(string Id,string DefinitionId,double X,double Y,int Rotation,
    IReadOnlyDictionary<string,int> Contents);

public sealed partial class GameSession
{
    public string SeatedFurnitureId {get;private set;}="";
    public int FurnitureRevision {get;private set;}
    FurnitureState Furniture=>Buildings.Furniture??=new();
    public IReadOnlyList<FurnitureView> FurnitureViews()=>Array.AsReadOnly((Buildings.Furniture?.Placed.Values ?? Enumerable.Empty<PlacedFurniture>())
        .Where(p=>p.BuildingId==ActiveBuildingId).OrderBy(p=>p.Id,StringComparer.Ordinal)
        .Select(p=>new FurnitureView(p.Id,p.DefinitionId,p.X,p.Y,p.Rotation,new ReadOnlyDictionary<string,int>(new Dictionary<string,int>(p.Contents)))).ToArray());
    public int CarriedCount(string item)=>Buildings.Furniture?.Carried.GetValueOrDefault(Player.Id)?.GetValueOrDefault(item)??0;
    public string NearestFurniture()=>Buildings.Furniture?.Placed.Values.Where(p=>p.BuildingId==ActiveBuildingId && Position.Distance(new(p.X,p.Y+.6))<=1.6)
        .OrderBy(p=>Position.Distance(new(p.X,p.Y+.6))).Select(p=>p.Id).FirstOrDefault()??"";
    string FurnitureError(Guid command)
    {
        CheckThread();
        if(command==Guid.Empty || Buildings.Furniture?.AppliedCommands.Contains(command)==true)return "คำสั่งไม่ถูกต้องหรือดำเนินการแล้ว";
        if(State.Paused || Player.Sleeping || SeatedFurnitureId!="")return "กลับเข้าเกมและยืนขึ้นก่อน";
        return "";
    }
    CommandResult FurnitureCommit(Guid command,string message)
    {
        Furniture.AppliedCommands.Add(command);if(Furniture.AppliedCommands.Count>256)Furniture.AppliedCommands.RemoveAt(0);
        FurnitureRevision++;BuildingChanged();log.Write("info","furniture.command");return new(message,true);
    }
    void SetCarried(string item,int count)
    {
        if(!Furniture.Carried.TryGetValue(Player.Id,out var bag))Furniture.Carried[Player.Id]=bag=new();
        if(count==0)bag.Remove(item);else bag[item]=count;
    }
    int InventoryCount(string item)=>ItemCatalog.Find(item)?.Category switch {
        ItemCategory.Produce=>Player.Bag.GetValueOrDefault(item),
        ItemCategory.Seed=>Buildings.Village.Seeds.GetValueOrDefault(ItemCatalog.Find(item)!.CropId!),
        _=>CarriedCount(item)
    };
    int InventoryLimit(string item)=>ItemCatalog.Find(item)?.Category switch {
        ItemCategory.Produce=>int.MaxValue,ItemCategory.Seed=>1000000,_=>FurnitureState.CarryLimit
    };
    void SetInventory(string item,int count)
    {
        var def=ItemCatalog.Find(item)!;
        if(def.Category==ItemCategory.Produce){if(count==0)Player.Bag.Remove(item);else Player.Bag[item]=count;}
        else if(def.Category==ItemCategory.Seed){if(count==0)Buildings.Village.Seeds.Remove(def.CropId!);else Buildings.Village.Seeds[def.CropId!]=count;}
        else SetCarried(item,count);
    }
    public CommandResult BuyFurniture(string item,int quantity,Guid command)
    {
        string error=FurnitureError(command);if(error!="")return new(error,false);
        error=ServiceError("rowan");if(error!="")return new(error,false);
        var def=PlaceableFurnitureCatalog.Find(item);int price=item==PlaceableFurnitureCatalog.WoodId?5:def?.Price??0;
        if(price==0 || (item!=PlaceableFurnitureCatalog.WoodId && item!=def?.ItemId) || quantity<1 || quantity>99 || (long)CarriedCount(item)+quantity>FurnitureState.CarryLimit || (long)price*quantity>State.Coins)return new("เงินไม่พอหรือจำนวนไม่ถูกต้อง",false);
        SetCarried(item,CarriedCount(item)+quantity);State.Coins-=price*quantity;return FurnitureCommit(command,"ซื้อสินค้าแล้ว · เปิดกระเป๋าเพื่อวางในบ้าน");
    }
    public string PreviewFurniture(string item,double x,double y,int rotation,string moving="")
    {
        CheckThread();
        if(ActiveBuildingId!=BuildingRegistry.Farmhouse.Id || State.Owner!=Player.Id)return "วางได้ในบ้านของคุณเท่านั้น";
        var def=PlaceableFurnitureCatalog.Find(item);if(def==null)return "ไม่พบเฟอร์นิเจอร์";
        if(moving!="" && (Buildings.Furniture?.Placed.GetValueOrDefault(moving) is not {} old || old.OwnerId!=Player.Id || old.DefinitionId!=def.Id))return "ไม่พบวัตถุที่ย้ายได้";
        if(moving!="" && FurnitureAccess(moving,out _)!="")return "เดินเข้าใกล้เฟอร์นิเจอร์ที่ต้องการย้ายก่อน";
        var objects=Buildings.Furniture?.Placed.Values.AsEnumerable()??Enumerable.Empty<PlacedFurniture>();
        if(moving=="" && objects.Count()>=FurnitureState.PlacementLimit)return "เฟอร์นิเจอร์ครบ 64 ชิ้นแล้ว";
        return FurniturePlacement.Error(new(){Id=moving,DefinitionId=def.Id,BuildingId=ActiveBuildingId,X=x,Y=y,Rotation=rotation},objects,Buildings.Locations.Values);
    }
    public CommandResult PlaceFurniture(string item,double x,double y,int rotation,Guid command,string moving="")
    {
        string error=FurnitureError(command);if(error!="")return new(error,false);
        error=PreviewFurniture(item,x,y,rotation,moving);if(error!="")return new(error,false);
        var def=PlaceableFurnitureCatalog.Find(item)!;
        if(moving=="" && CarriedCount(def.ItemId)<1)return new("ไม่มีเฟอร์นิเจอร์ในกระเป๋า",false);
        if(moving=="" && Furniture.Placed.ContainsKey(command.ToString("N")))return new("รหัสวัตถุนี้ใช้งานแล้ว",false);
        if(moving!="") {
            error=FurnitureAccess(moving,out var existing);if(error!="")return new(error,false);
            existing!.X=x;existing.Y=y;existing.Rotation=rotation;
        } else {
            SetCarried(def.ItemId,CarriedCount(def.ItemId)-1);
            var p=new PlacedFurniture{Id=command.ToString("N"),OwnerId=Player.Id,DefinitionId=def.Id,BuildingId=ActiveBuildingId,X=x,Y=y,Rotation=rotation};
            Furniture.Placed.Add(p.Id,p);
        }
        return FurnitureCommit(command,"วางเฟอร์นิเจอร์แล้ว · F เพื่อใช้งาน");
    }
    string FurnitureAccess(string id,out PlacedFurniture? placed)
    {
        placed=id==null?null:Buildings.Furniture?.Placed.GetValueOrDefault(id);
        if(placed==null || placed.BuildingId!=ActiveBuildingId || placed.OwnerId!=Player.Id || Position.Distance(new(placed.X,placed.Y+.6))>1.6)return "เดินเข้าใกล้เฟอร์นิเจอร์ของคุณก่อน";
        return "";
    }
    public CommandResult PickUpFurniture(string id,Guid command)
    {
        string error=FurnitureError(command);if(error!="")return new(error,false);
        error=FurnitureAccess(id,out var p);if(error!="")return new(error,false);
        var def=PlaceableFurnitureCatalog.Find(p!.DefinitionId)!;
        if(p.Contents.Count!=0)return new("นำของออกให้หมดก่อนเก็บเฟอร์นิเจอร์",false);
        if(CarriedCount(def.ItemId)>=FurnitureState.CarryLimit)return new("กระเป๋าเฟอร์นิเจอร์เต็ม",false);
        SetCarried(def.ItemId,CarriedCount(def.ItemId)+1);Furniture.Placed.Remove(id);return FurnitureCommit(command,"เก็บเฟอร์นิเจอร์เข้ากระเป๋าแล้ว");
    }
    public CommandResult TransferFurniture(string id,string item,int quantity,bool deposit,Guid command)
    {
        string error=FurnitureError(command);if(error!="")return new(error,false);
        error=FurnitureAccess(id,out var p);if(error!="")return new(error,false);
        var def=PlaceableFurnitureCatalog.Find(p!.DefinitionId)!;
        if(def.Interaction is not (PlacedInteraction.Storage or PlacedInteraction.Display) || item==null || !FurnitureState.Storable(item) || quantity<1)return new("ไม่รองรับรายการนี้",false);
        int stored=p.Contents.GetValueOrDefault(item),carried=InventoryCount(item);
        int limit=def.Interaction==PlacedInteraction.Display?1:FurnitureState.StackLimit(item);
        if(deposit ? carried<quantity || (long)stored+quantity>limit || stored==0 && p.Contents.Count>=def.Capacity : stored<quantity || (long)carried+quantity>InventoryLimit(item))return new("ของไม่พอหรือช่องเก็บเต็ม",false);
        int next=stored+(deposit?quantity:-quantity);if(next==0)p.Contents.Remove(item);else p.Contents[item]=next;
        SetInventory(item,carried+(deposit?-quantity:quantity));return FurnitureCommit(command,deposit?"จัดวางหรือฝากของแล้ว":"นำของกลับเข้ากระเป๋าแล้ว");
    }
    public CommandResult OrganizeFurniture(string id,Guid command)
    {
        string error=FurnitureError(command);if(error!="")return new(error,false);
        error=FurnitureAccess(id,out var p);if(error!="")return new(error,false);
        if(PlaceableFurnitureCatalog.Find(p!.DefinitionId)!.Interaction is not (PlacedInteraction.Storage or PlacedInteraction.Display))return new("ไม่มีช่องเก็บของ",false);
        p.Contents=p.Contents.OrderBy(pair=>pair.Key,StringComparer.Ordinal).ToDictionary(pair=>pair.Key,pair=>pair.Value);
        return FurnitureCommit(command,"จัดเรียงรายการแล้ว");
    }
    public CommandResult CraftFurniture(string workbench,string output,Guid command)
    {
        string error=FurnitureError(command);if(error!="")return new(error,false);
        error=FurnitureAccess(workbench,out var p);if(error!="")return new(error,false);
        var def=PlaceableFurnitureCatalog.Find(output);
        if(PlaceableFurnitureCatalog.Find(p!.DefinitionId)!.Interaction!=PlacedInteraction.Craft || def==null || CarriedCount(PlaceableFurnitureCatalog.WoodId)<def.WoodCost || CarriedCount(def.ItemId)>=FurnitureState.CarryLimit)return new("ต้องมีไม้และโต๊ะงานช่างที่ใช้งานได้",false);
        SetCarried(PlaceableFurnitureCatalog.WoodId,CarriedCount(PlaceableFurnitureCatalog.WoodId)-def.WoodCost);SetCarried(def.ItemId,CarriedCount(def.ItemId)+1);
        return FurnitureCommit(command,"สร้าง"+def.Name+"เข้ากระเป๋าแล้ว");
    }
    public CommandResult SitFurniture(string id)
    {
        CheckThread();if(State.Paused || Player.Sleeping)return new("กลับเข้าเกมก่อน",false);
        if(SeatedFurnitureId!=""){SeatedFurnitureId="";StateChanged?.Invoke(GameChange.Player);return new("ยืนขึ้นแล้ว",true);}
        string error=FurnitureAccess(id,out var p);if(error!="")return new(error,false);
        if(PlaceableFurnitureCatalog.Find(p!.DefinitionId)!.Interaction!=PlacedInteraction.Seat)return new("ไม่ใช่เก้าอี้",false);
        SeatedFurnitureId=id;StateChanged?.Invoke(GameChange.Player);return new("นั่งพัก · F เพื่อยืน",true);
    }
}
