using Godot;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;
public partial class FarmHud
{
    ItemCategory? bagFilter;
    bool bagQuantitySort;
    void AddBackpack()
    {
        var filters=new HBoxContainer();modalContent.AddChild(filters);
        foreach(var choice in new[]{("ทั้งหมด",(ItemCategory?)null),("ผลผลิต",(ItemCategory?)ItemCategory.Produce),("เมล็ด",(ItemCategory?)ItemCategory.Seed)}) {
            var selected=choice;var button=Button(choice.Item1,()=>{bagFilter=selected.Item2;OpenBag();},90);
            button.Name="BagFilter-"+(choice.Item2?.ToString()??"All");filters.AddChild(button);
        }
        var sort=Button(bagQuantitySort?"เรียงตามรหัส":"เรียงตามจำนวน",()=>{bagQuantitySort=!bagQuantitySort;OpenBag();},140);sort.Name="BagSort";filters.AddChild(sort);
        var stacks=farm.Session.Read.ItemStacks();
        modalContent.AddChild(new Label {Name="BagSlotSummary",Text=$"มี {stacks.Count} รายการ · เมล็ดใช้ก่อนเงินสดตอนปลูก",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        var grid=new GridContainer {Name="BackpackSlots",Columns=6,SizeFlagsHorizontal=SizeFlags.ExpandFill};modalContent.AddChild(grid);
        int index=0;
        foreach(var stack in farm.Session.Read.BackpackSlots(bagFilter,bagQuantitySort)) {
            if(stack==null) {grid.AddChild(new Button {Name="EmptySlot-"+index++,Disabled=true,CustomMinimumSize=new Vector2(72,62),TooltipText="ช่องว่าง"});continue;}
            var item=farm.Session.Read.ItemDefinitions[stack.ItemId];
            var card=farm.Art.ItemCardScene.Instantiate<FarmItemCard>();card.Name="Item-"+stack.ItemId.Replace(':','_');grid.AddChild(card);
            string description=stack.Quantity+" ชิ้น\n"+item.Description;
            if(item.Category==ItemCategory.Produce)description+="\nราคาวันนี้ "+farm.Session.Read.Inventory().Single(i=>i.Crop==item.ItemId).UnitPrice+" เหรียญ";
            card.Configure(item.Name,description,farm.Art.ItemIcon(item.Icon),()=> {
                if(item.Category==ItemCategory.Seed){farm.Seed=item.CropId!;CloseModal();farm.Notify("เลือก"+item.Name+"แล้ว");}
                else OpenItem(farm.Session.Read.Inventory().Single(i=>i.Crop==item.ItemId));
            });
            card.MakeInventorySlot(stack.Quantity);
        }
    }
}
