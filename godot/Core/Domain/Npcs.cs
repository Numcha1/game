using System.Text.Json.Serialization;
using SuanPleng;

namespace SeasonsTogether.Core.Domain;

public sealed record NpcDefinition(string Id,string Name,string Role,string Home,string Workplace,bool Implemented);
public static class NpcRegistry
{
    public const string Village="moonroot.village";
    public static IReadOnlyList<NpcDefinition> All {get;}=Array.AsReadOnly(new[]{
        new NpcDefinition("mira","มิรา","พ่อค้าเมล็ดพันธุ์","village.mira-home","village.seed-shop",true),
        new NpcDefinition("rowan","โรวัน","ช่างไม้","village.rowan-home","village.workshop",true),
        new NpcDefinition("theo","ธีโอ","ช่างตีเหล็ก","","",false),new NpcDefinition("luna","ลูนา","นักสมุนไพร","","",false),
        new NpcDefinition("finn","ฟินน์","ชาวประมง","","",false),new NpcDefinition("hazel","เฮเซล","ผู้ดูแลป่า","","",false),
        new NpcDefinition("clara","คลารา","คนทำขนม","","",false),new NpcDefinition("elias","อีไลอัส","ผู้ดูแลหมู่บ้าน","","",false),
        new NpcDefinition("iris","ไอริส","บรรณารักษ์","","",false),new NpcDefinition("bram","บราม","คนงานเหมือง","","",false),
        new NpcDefinition("selene","เซลีน","นักวิจัยฤดูกาล","","",false),new NpcDefinition("nia","เนีย","ผู้ดูแลสัตว์","","",false)});
    public static IEnumerable<NpcDefinition> Active=>All.Where(n=>n.Implemented);
    public static NpcDefinition? Find(string id)=>All.FirstOrDefault(n=>n.Id==id && n.Implemented);
}
public sealed class NpcState
{
    [JsonRequired] public string Region {get;set;}="";
    [JsonRequired] public double X {get;set;}
    [JsonRequired] public double Y {get;set;}
    [JsonRequired] public Dictionary<string,int> Friendship {get;set;}=new();
    [JsonRequired] public Dictionary<string,int> ConversationDay {get;set;}=new();
    [JsonRequired] public List<string> History {get;set;}=new();
    [JsonIgnore] public WorldPoint Position {get=>new(X,Y);set {X=value.X;Y=value.Y;}}
}
public sealed class VillageState
{
    [JsonRequired] public Dictionary<string,NpcState> Npcs {get;set;}=new();
    [JsonRequired] public Dictionary<string,int> Seeds {get;set;}=new();
    [JsonRequired] public List<Guid> AppliedCommands {get;set;}=new();
    public void Validate(Farm farm)
    {
        if(Npcs==null || Seeds==null || AppliedCommands==null || (Npcs.Count!=0 && Npcs.Count!=2) || AppliedCommands.Count>128 ||
           AppliedCommands.Any(g=>g==Guid.Empty) || AppliedCommands.Distinct().Count()!=AppliedCommands.Count ||
           Seeds.Any(p=>!Farm.Crops.ContainsKey(p.Key) || p.Value<1 || p.Value>1000000))throw new InvalidDataException("ข้อมูลหมู่บ้านไม่ถูกต้อง");
        foreach(var pair in Npcs) {
            var npc=pair.Value;
            if(NpcRegistry.Find(pair.Key)==null || npc==null || BuildingRegistry.Find(npc.Region) is not {} region ||
               !region.Walkable(npc.Position) || npc.Friendship==null || npc.ConversationDay==null || npc.History==null || npc.History.Count>20 ||
               npc.History.Any(h=>!NpcDialogue.Contains(h)) || npc.Friendship.Count>40 || npc.ConversationDay.Count>40 ||
               npc.Friendship.Any(p=>!farm.Players.Any(f=>f.Id==p.Key) || p.Value<0 || p.Value>1000) ||
               npc.ConversationDay.Any(p=>!farm.Players.Any(f=>f.Id==p.Key) || p.Value<1 || p.Value>farm.Day))
                throw new InvalidDataException("สถานะ NPC ไม่รองรับ ไม่มีการทิ้งข้อมูลเซฟ");
            var def=NpcRegistry.Find(pair.Key)!;
            if(npc.Region!=NpcRegistry.Village && npc.Region!=def.Home && npc.Region!=def.Workplace)throw new InvalidDataException("NPC อยู่ผิดอาคาร");
        }
    }
}
public readonly record struct NpcDestination(string Region,WorldPoint Position,string Activity);
public static class NpcSchedules
{
    // Pure schedule resolution. Future festival/quest policies can supply an explicit destination.
    public static NpcDestination Resolve(NpcDefinition npc,int minute,string weather,int season)
    {
        if(minute<480 || minute>=1140)return new(npc.Home,new(12,9),"พักผ่อนที่บ้าน");
        bool storm=weather is "พายุ" or "ฝนตก";
        if(!storm && (minute is >=720 and <780 || minute>=1020))
            return new(NpcRegistry.Village,npc.Id=="mira"?new(9,10):new(15,10),season==3?"พักรับแดดฤดูหนาว":"พักที่ลานหมู่บ้าน");
        return new(npc.Workplace,new(12,8),"ทำงาน");
    }
}
public static class NpcNavigation
{
    public static IReadOnlyList<WorldPoint> FindPath(BuildingDefinition region,WorldPoint from,WorldPoint to)
    {
        if(!region.Walkable(from) || !region.Walkable(to))return Array.Empty<WorldPoint>();
        const double step=.5;
        (int X,int Y) Cell(WorldPoint p)=>((int)Math.Round(p.X/step),(int)Math.Round(p.Y/step));
        WorldPoint Point((int X,int Y) c)=>new(c.X*step,c.Y*step);
        var start=Cell(from);var goal=Cell(to);var queue=new Queue<(int X,int Y)>();queue.Enqueue(start);
        var previous=new Dictionary<(int X,int Y),(int X,int Y)> {[start]=start};
        while(queue.Count>0 && previous.Count<4096) {
            var current=queue.Dequeue();if(current==goal) {
                var result=new List<WorldPoint>();for(var cell=goal;cell!=start;cell=previous[cell])result.Add(Point(cell));
                result.Reverse();result.Add(to);return result;
            }
            foreach(var next in new[]{(current.X+1,current.Y),(current.X-1,current.Y),(current.X,current.Y+1),(current.X,current.Y-1)})
                if(!previous.ContainsKey(next) && region.Walkable(Point(next))) {previous[next]=current;queue.Enqueue(next);}
        }
        return Array.Empty<WorldPoint>();
    }
}
public sealed class NpcSimulation
{
    readonly Dictionary<string,(string Region,WorldPoint Target,Queue<WorldPoint> Path)> routes=new();
    public void Initialize(VillageState village,Farm farm)
    {
        foreach(var def in NpcRegistry.Active)if(!village.Npcs.ContainsKey(def.Id)) {
            var destination=NpcSchedules.Resolve(def,(int)farm.Minute,farm.Weather,farm.Season);
            village.Npcs[def.Id]=new NpcState {Region=destination.Region,X=destination.Position.X,Y=destination.Position.Y};
        }
    }
    public void Advance(VillageState village,Farm farm,string visible,double seconds)
    {
        if(village.Npcs.Count==0 || farm.Paused)return;
        foreach(var def in NpcRegistry.Active) {
            var npc=village.Npcs[def.Id];var destination=NpcSchedules.Resolve(def,(int)farm.Minute,farm.Weather,farm.Season);
            if(visible!=NpcRegistry.Village && visible!=npc.Region && visible!=destination.Region) {
                npc.Region=destination.Region;npc.Position=destination.Position;routes.Remove(def.Id);continue;
            }
            WorldPoint target=destination.Position;
            if(npc.Region!=destination.Region)target=npc.Region==NpcRegistry.Village ? BuildingRegistry.Find(destination.Region)!.Entrance : BuildingRegistry.Find(npc.Region)!.Exit;
            if(npc.Position.Distance(target)<.05) {
                if(npc.Region!=destination.Region) {
                    if(npc.Region==NpcRegistry.Village){npc.Region=destination.Region;npc.Position=BuildingRegistry.Find(npc.Region)!.Spawn;}
                    else {var building=BuildingRegistry.Find(npc.Region)!;npc.Region=NpcRegistry.Village;npc.Position=building.Return;}
                    routes.Remove(def.Id);
                }
                continue;
            }
            if(!routes.TryGetValue(def.Id,out var route) || route.Region!=npc.Region || route.Target!=target) {
                route=(npc.Region,target,new Queue<WorldPoint>(NpcNavigation.FindPath(BuildingRegistry.Find(npc.Region)!,npc.Position,target)));routes[def.Id]=route;
            }
            double budget=Math.Min(seconds,.25)*1.65;
            while(budget>0 && route.Path.TryPeek(out var next)) {
                double distance=npc.Position.Distance(next);
                if(distance<=budget){npc.Position=next;budget-=distance;route.Path.Dequeue();}
                else {var p=npc.Position;npc.Position=new(p.X+(next.X-p.X)/distance*budget,p.Y+(next.Y-p.Y)/distance*budget);break;}
            }
        }
    }
}
public static class NpcDialogue
{
    static readonly IReadOnlyDictionary<string,string> Thai=new Dictionary<string,string> {
        ["mira.greeting"]="สวัสดี! ฉันมิรา ดูแลร้านเมล็ดพันธุ์ ถ้าไม่แน่ใจว่าจะปลูกอะไร มาคุยกันได้นะ",
        ["rowan.greeting"]="สวัสดี ฉันโรวัน ช่างไม้ประจำหมู่บ้าน งานที่แข็งแรงเริ่มจากการวางแผนที่ดี",
        ["mira.guide"]="รดน้ำทุกวัน สลับชนิดพืชเพื่อถนอมดิน และดูฤดูก่อนปลูก เมล็ดที่ซื้อไว้จะใช้ก่อนเงินสดตอนปลูก",
        ["rowan.guide"]="ฉันติดตั้งหลังคาเรือนกระจกให้ได้ ใช้ 300 เหรียญ ปลูกได้ทุกฤดูและกันพายุ ตามกฎเดิมของฟาร์ม",
        ["mira.request"]="ฉันอยากเห็นแครอทจากสวนของเธอ! ตอนนี้คุยเรื่องการปลูกกันได้ ระบบส่งคำขอและรางวัลจะมาในช่วงเควสต์",
        ["rowan.project"]="บ้านต้องค่อย ๆ เติบโต ระบบขยายห้องและจัดเฟอร์นิเจอร์ยังไม่เปิด วันนี้ฉันช่วยเรื่องเรือนกระจกได้",
        ["mira.rain"]="ฝนวันนี้ช่วยรดน้ำ แต่ระวังดินแฉะนะ ฉันจะอยู่ในร้านแทนการไปพักที่ลาน",
        ["rowan.rain"]="ฝนลงแล้ว เก็บเครื่องมือให้แห้งก่อน ฉันทำงานอยู่ในโรงไม้",
        ["mira.friend"]="ดีใจที่ได้เจอชาวสวนที่ตั้งใจอย่างเธออีก วันนี้สวนเป็นอย่างไรบ้าง?",
        ["rowan.friend"]="เห็นเธอมาคุยเสมอแล้วอุ่นใจ เราจะค่อย ๆ ทำให้หมู่บ้านน่าอยู่ขึ้น",
        ["mira.winter"]="ฤดูหนาวต้องวางแผนให้ดี ดูรายการฤดูที่ปลูกได้ก่อนจ่ายเงินนะ",
        ["rowan.winter"]="อากาศเย็นแล้ว หลังคาดี ๆ ช่วยให้สวนผ่านฤดูนี้ได้",
        ["mira.home"]="ร้านปิดแล้ว วันนี้ขอพักที่บ้านก่อน พรุ่งนี้หลังแปดโมงแวะมาได้นะ",
        ["rowan.home"]="เลิกงานแล้ว ขอพักมือสักหน่อย พรุ่งนี้ฉันกลับไปทำงานตั้งแต่แปดโมง"
    };
    public static bool Contains(string key)=>Thai.ContainsKey(key);
    public static string Text(string key)=>Thai.GetValueOrDefault(key,"ขอโทษนะ ตอนนี้ฉันยังไม่มีเรื่องใหม่ แต่เธอกลับออกไปเล่นต่อได้เสมอ");
}
