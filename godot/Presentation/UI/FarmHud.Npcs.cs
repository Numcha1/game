using Godot;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;

public partial class FarmHud
{
    void AppendSeedInventory()
    {
        var seeds=GameCatalog.CropKeys.Where(c=>farm.Session.SeedCount(c)>0).ToArray();
        if(seeds.Length==0)return;
        modalContent.AddChild(new Label {Text="เมล็ดที่ซื้อไว้ · ใช้ก่อนเงินสดตอนปลูก",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        foreach(string crop in seeds)modalContent.AddChild(new Label {Text=GameCatalog.Crops[crop].Name+" · "+farm.Session.SeedCount(crop)+" ถุง",AutowrapMode=TextServer.AutowrapMode.WordSmart});
    }
    public string NpcDialogueText {get;private set;}="";
    public void OpenNpc(string id)
    {
        string error=farm.Session.NpcAccessError(id);if(error!=""){farm.Notify(error);return;}
        var def=NpcRegistry.Find(id)!;Open(def.Name+" · "+def.Role);
        currentMenu=0;
        var header=new HBoxContainer {SizeFlagsHorizontal=SizeFlags.ExpandFill};modalContent.AddChild(header);
        header.AddChild(new TextureRect {Name="NpcPortrait",Texture=NpcAssets.Pose(id),CustomMinimumSize=new Vector2(90,90),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,TextureFilter=TextureFilterEnum.Nearest});
        var text=new Label {Name="NpcDialogue",AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new Vector2(330,95)};header.AddChild(text);
        void Say(string choice) {NpcDialogueText=farm.Session.Dialogue(id,choice);text.Text=NpcDialogueText;}
        Say("greeting");
        void Choice(string label,string key) {var button=Button(label,()=>Say(key));button.Name="NpcChoice-"+key;modalContent.AddChild(button);}
        Choice("คุยเรื่องการทำฟาร์มและงานของคุณ","guide");
        Choice(id=="mira"?"มีผลผลิตอะไรที่คุณสนใจ?":"คุยเรื่องขยายบ้าน",id=="mira"?"request":"project");
        if(farm.Session.TryNpc(id,out var npc)) {
            modalContent.AddChild(new Label {Text=npc.Activity+" · มิตรภาพ "+npc.Hearts+" / 10 หัวใจ",AutowrapMode=TextServer.AutowrapMode.WordSmart});
            bool work=npc.Region==def.Workplace;
            if(work && id=="mira") {
                modalContent.AddChild(new Label {Text="ซื้อเก็บไว้ก่อนปลูก · ใช้เมล็ดที่ซื้อไว้ก่อนเงินสด\nเลือกได้ทุกฤดู ตรวจฤดูที่ปลูกได้ก่อนซื้อ",AutowrapMode=TextServer.AutowrapMode.WordSmart});
                var quantity=new SpinBox {Name="SeedQuantity",MinValue=1,MaxValue=99,Value=1,Step=1,CustomMinimumSize=new Vector2(150,32)};modalContent.AddChild(quantity);
                foreach(var offer in farm.Session.Read.ShopOffers(id)) {
                    string crop=farm.Session.Read.ItemDefinitions[offer.ItemId].CropId!;var info=GameCatalog.Crops[crop];Guid command=Guid.NewGuid();
                    var button=Button($"ซื้อ {info.Name} · {offer.UnitBuyPrice} เหรียญ/ถุง · มี {farm.Session.SeedCount(crop)}",()=> {
                        var result=farm.Session.BuySeeds(crop,(int)quantity.Value,command);farm.Notify(result.Message);
                        if(result.Changed)OpenNpc(id);
                    });button.Name="BuySeed-"+crop;modalContent.AddChild(button);
                }
            }
            if(work && id=="rowan") {
                var furnitureShop=Button("ซื้อเฟอร์นิเจอร์และไม้แปรรูป",OpenFurnitureShop);furnitureShop.Name="OpenFurnitureShop";modalContent.AddChild(furnitureShop);
                Guid command=Guid.NewGuid();var button=Button(farm.Session.Read.World.Greenhouse?"ติดตั้งเรือนกระจกแล้ว":"ติดตั้งหลังคาเรือนกระจก · 300 เหรียญ",()=> {
                    var result=farm.Session.RequestCarpentry(command);farm.Notify(result.Message);if(result.Changed)OpenNpc(id);
                });button.Name="NpcCarpentry";button.Disabled=farm.Session.Read.World.Greenhouse;modalContent.AddChild(button);
            }
        }
        modalContent.AddChild(Button("จบบทสนทนา",CloseModal));
    }
}
