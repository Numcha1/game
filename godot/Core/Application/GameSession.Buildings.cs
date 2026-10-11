using SeasonsTogether.Core.Domain;

namespace SeasonsTogether.Core.Application;

public sealed partial class GameSession
{
    internal BuildingState Buildings { get; }
    readonly Dictionary<Guid,CommandResult> recentStorageCommands=new();
    public string ActiveBuildingId => Buildings.Locations.GetValueOrDefault(Player.Id)?.BuildingId ?? "";
    public WorldPoint Position => Buildings.Locations.TryGetValue(Player.Id,out var indoor) ? new(indoor.X,indoor.Y) : new(Player.X,Player.Y);
    public bool InsideBuilding => ActiveBuildingId != "";
    public string EntryError(BuildingDefinition building)
    {
        CheckThread();
        if(InsideBuilding)return "อยู่ในอาคารแล้ว";
        if(State.Paused || Player.Sleeping)return "กลับเข้าเกมก่อนเปิดประตู";
        if(Position.Distance(building.Entrance)>1.8)return "เดินเข้าใกล้ประตูก่อน";
        if(building.OwnerOnly && State.Owner!=Player.Id)return "เจ้าของบ้านเท่านั้นที่เข้าได้";
        if(State.Minute<building.Opens || State.Minute>=building.Closes)return "อาคารปิดในเวลานี้";
        if(!building.Walkable(building.Spawn) || !SuanPleng.Farm.Walkable(building.Return.X,building.Return.Y))return "ทางเข้าอาคารไม่พร้อม";
        return "";
    }
    public CommandResult EnterBuilding(string id)
    {
        CheckThread();
        var building=BuildingRegistry.Find(id);
        if(building==null)return new("ไม่พบอาคารนี้",false);
        string error=EntryError(building);if(error!="")return new(error,false);
        Buildings.Interiors.TryAdd(id,new InteriorState());
        Buildings.Locations[Player.Id]=new InteriorLocation { BuildingId=id,X=building.Spawn.X,Y=building.Spawn.Y };
        Player.X=building.Return.X;Player.Y=building.Return.Y;
        BuildingChanged();return new("เข้าบ้านแล้ว · F โต้ตอบกับเตียง กล่อง หรือประตู",true);
    }
    public CommandResult ExitBuilding(bool emergency=false)
    {
        CheckThread();
        if(BuildingRegistry.Find(ActiveBuildingId) is not { } building)return new("ไม่ได้อยู่ในอาคาร",false);
        if(!emergency && Position.Distance(building.Exit)>1.4)return new("เดินเข้าใกล้ประตูทางออกก่อน",false);
        if(!SuanPleng.Farm.Walkable(building.Return.X,building.Return.Y))return new("จุดกลับฟาร์มไม่พร้อม",false);
        Buildings.Locations.Remove(Player.Id);Player.X=building.Return.X;Player.Y=building.Return.Y;
        BuildingChanged();return new("กลับฟาร์มแล้ว",true);
    }
    void BuildingChanged()
    {
        saves.RequestSave(State,Buildings);
        StateChanged?.Invoke(GameChange.World | GameChange.Position | GameChange.Inventory);
    }
    public string StorageAccessError()
    {
        CheckThread();
        if(State.Paused || Player.Sleeping)return "กลับเข้าเกมก่อนเปิดกล่อง";
        if(BuildingRegistry.Find(ActiveBuildingId) is not { } building || Position.Distance(building.Chest)>1.6)return "เดินเข้าใกล้กล่องเก็บของในบ้านก่อน";
        return "";
    }
    public CommandResult TransferStorage(string crop,int amount,bool deposit,Guid id)
    {
        CheckThread();
        if(id==Guid.Empty)return new("คำสั่งโอนของไม่มีรหัส",false);
        if(recentStorageCommands.TryGetValue(id,out var previous))return previous with{Changed=false};
        CommandResult Apply() {
            string error=StorageAccessError();if(error!="")return new(error,false);
            if(crop==null || !GameCatalog.Crops.ContainsKey(crop) || amount<=0)return new("เลือกผลผลิตและจำนวนให้ถูกต้อง",false);
            var chest=Buildings.Interiors[ActiveBuildingId].Storage;
            var source=deposit?Player.Bag:chest;var target=deposit?chest:Player.Bag;
            int available=source.GetValueOrDefault(crop),existing=target.GetValueOrDefault(crop);
            if(available<amount || (long)existing+amount>(deposit?InteriorState.MaximumStorageStack:int.MaxValue))return new("ของไม่พอหรือช่องเก็บของเต็ม",false);
            int remaining=available-amount;if(remaining==0)source.Remove(crop);else source[crop]=remaining;
            target[crop]=existing+amount;
            BuildingChanged();log.Write("info",deposit?"storage.deposit":"storage.withdraw");
            return new((deposit?"เก็บ":"นำออก")+" "+GameCatalog.Crops[crop].Name+" "+amount+" ชิ้น",true);
        }
        var result=Apply();recentStorageCommands[id]=result;
        if(recentStorageCommands.Count>512)recentStorageCommands.Remove(recentStorageCommands.Keys.First());
        return result;
    }
    public CommandResult ToggleInteriorLights()
    {
        CheckThread();
        if(!InsideBuilding || State.Paused || Player.Sleeping)return new("กลับเข้าเกมในบ้านก่อนปรับไฟ",false);
        var state=Buildings.Interiors[ActiveBuildingId];state.LightsOn=!state.LightsOn;BuildingChanged();
        return new(state.LightsOn?"เปิดไฟในบ้าน":"ปิดไฟในบ้าน",true);
    }
}
