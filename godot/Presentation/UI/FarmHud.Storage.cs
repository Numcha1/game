using Godot;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;

public partial class FarmHud
{
    public bool StorageModalVisible=>ModalVisible && currentMenu==3;
    public void OpenStorage()
    {
        string error=farm.Session.StorageAccessError();if(error!=""){farm.Notify(error);return;}
        Open("กล่องเก็บผลผลิตในบ้าน");
        modalContent.AddChild(new Label {Text="เลือกจำนวนแล้วฝากหรือนำออก · ของในกล่องไม่ลดจำนวนข้ามคืน\nผลผลิตในกระเป๋ายังคงลด 25% ตามกฎเดิม",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        var bag=farm.Session.Read.Inventory().ToDictionary(item=>item.Crop);
        var chest=farm.Session.Read.Storage().ToDictionary(item=>item.Crop);
        var grid=new GridContainer {Columns=2,SizeFlagsHorizontal=SizeFlags.ExpandFill};modalContent.AddChild(grid);
        foreach(string crop in GameCatalog.CropKeys) {
            int bagCount=bag.GetValueOrDefault(crop).Quantity,chestCount=chest.GetValueOrDefault(crop).Quantity;
            if(bagCount==0 && chestCount==0)continue;
            var panel=new PanelContainer {SizeFlagsHorizontal=SizeFlags.ExpandFill};grid.AddChild(panel);
            var rows=new VBoxContainer();panel.AddChild(rows);
            rows.AddChild(new Label {Text=GameCatalog.Crops[crop].Name,ThemeTypeVariation="ItemHeading"});
            rows.AddChild(new Label {Text=$"กระเป๋า {bagCount:N0} · กล่อง {chestCount:N0}",AutowrapMode=TextServer.AutowrapMode.WordSmart});
            var quantity=new SpinBox {MinValue=1,MaxValue=Math.Max(bagCount,chestCount),Step=1,Value=1,Suffix=" ชิ้น",CustomMinimumSize=new Vector2(185,30)};rows.AddChild(quantity);
            void AddTransfer(bool deposit,int available) {
                Guid id=Guid.NewGuid();
                var button=Button(deposit?"ฝากในกล่อง":"นำออกใส่กระเป๋า",()=>{
                    var result=farm.Session.TransferStorage(crop,(int)quantity.Value,deposit,id);farm.Notify(result.Message);
                });button.Name=(deposit?"StorageDeposit-":"StorageWithdraw-")+crop;button.Disabled=available==0;rows.AddChild(button);
            }
            AddTransfer(true,bagCount);AddTransfer(false,chestCount);
        }
        if(grid.GetChildCount()==0)modalContent.AddChild(new Label {Text="ยังไม่มีผลผลิตให้จัดเก็บ · เก็บเกี่ยวจากฟาร์มแล้วนำมาฝากได้",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        modalContent.AddChild(Button(farm.Session.Read.InteriorLightsOn?"ปิดไฟในบ้าน":"เปิดไฟในบ้าน",()=>farm.Notify(farm.Session.ToggleInteriorLights().Message)));
        currentMenu=3;menuDay=farm.Session.Read.World.Day;
    }
}
