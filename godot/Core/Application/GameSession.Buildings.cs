using SeasonsTogether.Core.Domain;

namespace SeasonsTogether.Core.Application;

public sealed partial class GameSession
{
    internal BuildingState Buildings { get; }
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
}
