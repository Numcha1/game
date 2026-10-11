using SeasonsTogether.Core.Domain;
using SuanPleng;

namespace SeasonsTogether.Core.Application;

public readonly record struct NpcView(string Id,string Name,string Role,string Region,double X,double Y,string Activity,int Hearts);
public sealed partial class GameSession
{
    readonly NpcSimulation npcSimulation=new();
    public IReadOnlyList<NpcView> Npcs()=>Array.AsReadOnly(Buildings.Village.Npcs.Select(pair=>{
        var def=NpcRegistry.Find(pair.Key)!;var n=pair.Value;
        return new NpcView(def.Id,def.Name,def.Role,n.Region,n.X,n.Y,NpcSchedules.Resolve(def,(int)State.Minute,State.Weather,State.Season).Activity,n.Friendship.GetValueOrDefault(Player.Id)/100);
    }).ToArray());
    public int SeedCount(string crop)=>Buildings.Village.Seeds.GetValueOrDefault(crop);
    public string NearestNpc()=>Npcs().Where(n=>n.Region==ActiveBuildingId && Position.Distance(new(n.X,n.Y))<=1.7)
        .OrderBy(n=>Position.Distance(new(n.X,n.Y))).Select(n=>n.Id).FirstOrDefault() ?? "";
    public string NpcAccessError(string id)
    {
        CheckThread();
        if(State.Paused || Player.Sleeping)return "กลับเข้าเกมก่อนพูดคุย";
        if(!Buildings.Village.Npcs.TryGetValue(id,out var n) || n.Region!=ActiveBuildingId || Position.Distance(n.Position)>1.7)return "เดินเข้าใกล้ตัวละครก่อน";
        return "";
    }
    public string Dialogue(string id,string choice="greeting")
    {
        CheckThread();string error=NpcAccessError(id);if(error!="")return error;
        if(choice is not ("greeting" or "guide" or "request" or "project"))return NpcDialogue.Text("");
        var n=Buildings.Village.Npcs[id];string key=id+"."+choice;
        if(choice=="greeting") {
            key=n.Region==NpcRegistry.Find(id)!.Home?id+".home":State.Weather is "ฝนตก" or "พายุ"?id+".rain":State.Season==3?id+".winter":n.Friendship.GetValueOrDefault(Player.Id)>=100?id+".friend":key;
            if(n.ConversationDay.GetValueOrDefault(Player.Id)!=State.Day) {
                n.ConversationDay[Player.Id]=State.Day;n.Friendship[Player.Id]=Math.Min(1000,n.Friendship.GetValueOrDefault(Player.Id)+10);
            }
        }
        if(NpcDialogue.Contains(key)) {n.History.Add(key);if(n.History.Count>20)n.History.RemoveAt(0);BuildingChanged();}
        return NpcDialogue.Text(key);
    }
    string ServiceError(string id)
    {
        string error=NpcAccessError(id);if(error!="")return error;
        var def=NpcRegistry.Find(id)!;
        if(ActiveBuildingId!=def.Workplace || NpcSchedules.Resolve(def,(int)State.Minute,State.Weather,State.Season).Region!=def.Workplace)return "บริการเปิดตอนตัวละครทำงานในร้านเท่านั้น";
        return "";
    }
    bool SeenVillageCommand(Guid id)=>Buildings.Village.AppliedCommands.Contains(id);
    void RecordVillageCommand(Guid id)
    {
        Buildings.Village.AppliedCommands.Add(id);if(Buildings.Village.AppliedCommands.Count>128)Buildings.Village.AppliedCommands.RemoveAt(0);
    }
    public CommandResult BuySeeds(string crop,int quantity,Guid id)
    {
        CheckThread();if(id==Guid.Empty || SeenVillageCommand(id))return new("คำสั่งซื้อไม่ถูกต้องหรือดำเนินการแล้ว",false);
        string error=ServiceError("mira");if(error!="")return new(error,false);
        if(crop==null || !Farm.Crops.TryGetValue(crop,out var seed) || quantity<1 || quantity>99)return new("เลือกเมล็ดและจำนวน 1–99",false);
        long cost=(long)seed.Cost*quantity;int count=SeedCount(crop);
        if(cost>State.Coins || count+quantity>1000000)return new("เหรียญไม่พอหรือเมล็ดเต็ม",false);
        State.Coins-=(int)cost;Buildings.Village.Seeds[crop]=count+quantity;RecordVillageCommand(id);BuildingChanged();
        return new("ซื้อเมล็ด"+seed.Name+" "+quantity+" ถุงแล้ว",true);
    }
    public CommandResult RequestCarpentry(Guid id)
    {
        CheckThread();if(id==Guid.Empty || SeenVillageCommand(id))return new("คำสั่งก่อสร้างไม่ถูกต้องหรือดำเนินการแล้ว",false);
        string error=ServiceError("rowan");if(error!="")return new(error,false);
        // Existing server-authoritative rule owns price, ownership, balance and effect.
        double x=Player.X,y=Player.Y;int coins=State.Coins;string message;
        try {Player.X=20;Player.Y=4;message=State.Action(Player,"greenhouse",-1,"carrot");}
        finally {Player.X=x;Player.Y=y;}
        bool changed=coins!=State.Coins;
        if(changed){RecordVillageCommand(id);BuildingChanged();}
        return new(message,changed);
    }
    string ApplyFarmCommand(GameCommand command)
    {
        if(command.Action!="plant" || SeedCount(command.Seed)==0)return State.Action(Player,command.Action,command.Plot,command.Seed);
        int cost=Farm.Crops[command.Seed].Cost,coins=State.Coins,energy=Player.Energy;
        if(coins>int.MaxValue-cost)return "ยอดเหรียญสูงเกินกว่าจะประมวลผลเมล็ดได้";
        State.Coins+=cost;
        try {
            string message=State.Action(Player,command.Action,command.Plot,command.Seed);
            if(Player.Energy<energy) {
                int remaining=SeedCount(command.Seed)-1;
                if(remaining==0)Buildings.Village.Seeds.Remove(command.Seed);else Buildings.Village.Seeds[command.Seed]=remaining;
            }
            return message;
        }
        finally {State.Coins=coins;}
    }
}
