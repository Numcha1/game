using Godot;
using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Application;

namespace SeasonsTogether;
public partial class FarmHud
{
    public void OpenFurnitureShop()
    {
        if(farm.Session.NpcAccessError("rowan")!=""){farm.Notify("เดินเข้าใกล้โรวานก่อน");return;}
        Open("งานไม้ของโรวาน");currentMenu=0;
        var quantity=new SpinBox {Name="FurnitureBuyQuantity",MinValue=1,MaxValue=99,Value=1,Step=1};modalContent.AddChild(quantity);
        foreach(var offer in farm.Session.Read.ShopOffers("rowan")) {
            var item=ItemCatalog.Find(offer.ItemId)!;var command=Guid.NewGuid();
            var row=new HBoxContainer();modalContent.AddChild(row);
            row.AddChild(new TextureRect {Texture=farm.Art.ItemIcon(item.Icon),CustomMinimumSize=new Vector2(48,50),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered});
            var button=Button(item.Name+" · "+offer.UnitBuyPrice+" เหรียญ · มี "+farm.Session.CarriedCount(item.ItemId),()=>{
                var result=farm.Session.BuyFurniture(item.ItemId,(int)quantity.Value,command);farm.Notify(result.Message);if(result.Changed)OpenFurnitureShop();
            });button.Name="BuyFurniture-"+item.ItemId.Replace(':','_');row.AddChild(button);
        }
        modalContent.AddChild(new Label {Text="ซื้อแล้วเปิดกระเป๋า เลือกเฟอร์นิเจอร์ และคลิกวางในบ้าน\nไม้แปรรูปใช้สร้างของที่โต๊ะงานช่าง",AutowrapMode=TextServer.AutowrapMode.WordSmart});
    }
    public void OpenFurniture(string id)
    {
        var view=farm.Session.FurnitureViews().FirstOrDefault(p=>p.Id==id);if(view==null)return;
        var def=PlaceableFurnitureCatalog.Find(view.DefinitionId)!;
        Open(def.Name);currentMenu=0;
        void ActionButton(string name,string label,Func<Guid,CommandResult> action,bool close=false)
        {
            Guid command=Guid.NewGuid();var button=Button(label,()=>{var result=action(command);farm.Notify(result.Message);if(result.Changed){if(close)CloseModal();else OpenFurniture(id);}});button.Name=name;modalContent.AddChild(button);
        }
        if(def.Interaction==PlacedInteraction.Seat) {
            var sit=Button("นั่งพัก · F เพื่อยืน",()=>{var result=farm.Session.SitFurniture(id);farm.Notify(result.Message);if(result.Changed)CloseModal();});sit.Name="FurnitureSit";modalContent.AddChild(sit);
        }
        if(def.Interaction is PlacedInteraction.Storage or PlacedInteraction.Display) {
            modalContent.AddChild(new Label {Name="FurnitureContentsSummary",Text=$"ใช้ {view.Contents.Count}/{def.Capacity} ช่อง · "+(def.Interaction==PlacedInteraction.Display?"แสดงหนึ่งชิ้นต่อรายการ":"ของที่ฝากไม่เสียข้ามคืน"),AutowrapMode=TextServer.AutowrapMode.WordSmart});
            var amount=new SpinBox {Name="FurnitureTransferQuantity",MinValue=1,MaxValue=def.Interaction==PlacedInteraction.Display?1:1000000,Step=1,Value=1};modalContent.AddChild(amount);
            foreach(var item in farm.Session.Read.ItemStacks().Where(p=>FurnitureState.Storable(p.ItemId))) {
                var selected=item;
                ActionButton("FurnitureDeposit-"+item.ItemId.Replace(':','_'),"ฝาก / แสดง "+ItemCatalog.Find(item.ItemId)!.Name+" · มี "+item.Quantity,c=>farm.Session.TransferFurniture(id,selected.ItemId,(int)amount.Value,true,c));
            }
            foreach(var pair in view.Contents) {
                var selected=pair;
                ActionButton("FurnitureWithdraw-"+pair.Key.Replace(':','_'),"นำออก "+ItemCatalog.Find(pair.Key)!.Name+" · "+pair.Value,c=>farm.Session.TransferFurniture(id,selected.Key,(int)amount.Value,false,c));
            }
            ActionButton("FurnitureOrganize","จัดเรียงรายการ",c=>farm.Session.OrganizeFurniture(id,c));
        }
        if(def.Interaction==PlacedInteraction.Craft) {
            modalContent.AddChild(new Label {Text="ไม้แปรรูปในกระเป๋า: "+farm.Session.CarriedCount(PlaceableFurnitureCatalog.WoodId)});
            foreach(var recipe in PlaceableFurnitureCatalog.All) {
                var selected=recipe;ActionButton("FurnitureCraft-"+recipe.Id,"สร้าง"+recipe.Name+" · ไม้ "+recipe.WoodCost,c=>farm.Session.CraftFurniture(id,selected.ItemId,c));
            }
        }
        var move=Button(def.Rotatable?"ย้าย / หมุน":"ย้าย",()=>{CloseModal();farm.BeginFurniturePlacement(def.ItemId,id);});move.Name="FurnitureMove";modalContent.AddChild(move);
        ActionButton("FurniturePickup","เก็บเข้ากระเป๋า (นำของออกก่อน)",c=>farm.Session.PickUpFurniture(id,c),true);
    }
}
