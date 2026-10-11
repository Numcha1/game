using SeasonsTogether.Core;
using SuanPleng;
using System.Text.Json;

string output=Path.GetFullPath(args.Length>0?args[0]:"reports/core-tests.json");Directory.CreateDirectory(Path.GetDirectoryName(output)!);
string directory=Path.Combine(Path.GetDirectoryName(output)!,"core-profile-"+Guid.NewGuid().ToString("N"));
var passed=new List<string>();void Check(bool condition,string message){if(!condition)throw new Exception(message);passed.Add(message);}
using(var session=new LocalFarmSession(directory)){
    var f=session.State;var p=session.Player;
    Check(f.Version==1&&f.Plots.Count==24,"v1 schema and 24 plots");
    Check(Farm.Crops.Count==8,"all eight crops");
    Check(!session.Move(3,2)&&!session.Move(18,13),"original house and pond collision preserved");
    session.Move(8,6);int coins=f.Coins;session.Execute("plant",0,"carrot");Check(f.Coins==coins-8&&p.Energy==95,"plant price and energy");
    session.Execute("water",0);Check(f.Plots[0].Moisture==95&&p.Energy==90,"water +45 capped100");
    session.Execute("fertilize",0);Check(f.Plots[0].Fertility==100,"fertility capped100");
    session.Execute("stake",0);Check(f.Plots[0].Stake,"storm stake");
    for(int i=0;i<3;i++){f.Weather="แดดอ่อน";f.Plots[0].Moisture=60;f.NextDay();}
    Check(f.Plots[0].Growth==3&&f.Plots[0].RipeDay==4,"carrot growth and ripe day");
    session.Move(8,6);session.Execute("harvest",0);Check(p.Bag.GetValueOrDefault("carrot")==3&&f.Plots[0].Crop=="","quality100 yields3");
    session.Move(20,8);coins=f.Coins;int expected=f.SalePrice("carrot")*3;session.Execute("sell");Check(f.Coins==coins+expected&&p.Bag.Count==0,"sale uses daily original price");
    for(int season=0;season<4;season++){f.Day=1+14*season;Check(f.Season==season&&f.SeasonDay==1,"14-day season"+season);}
    f.Day=1;session.Move(8,6);coins=f.Coins;session.Execute("plant",0,"pumpkin");Check(f.Coins==coins&&f.Plots[0].Crop=="","out-of-season plant rejected");
    session.Execute("plant",0,"carrot");f.Plots[0].LastCrop="carrot";f.Plots[0].Crop="";int fertility=f.Plots[0].Fertility;session.Execute("plant",0,"carrot");Check(f.Plots[0].Fertility==Math.Max(0,fertility-15),"rotation penalty");
    f.Weather="พายุ";f.Plots[0].Moisture=95;f.Plots[0].Stake=false;f.NextDay();Check(f.Plots[0].Disease&&f.Plots[0].Quality==45,"storm and overwatering disease");
    f.Plots[0].Quality=100;f.Plots[0].Disease=false;f.Plots[0].RipeDay=1;f.Day=8;f.NextDay();Check(f.Plots[0].Crop=="","late harvest spoils");
    p.Bag["carrot"]=4;f.NextDay();Check(p.Bag["carrot"]==3,"25percent overnight bag spoilage");
    f.Minute=360;session.Tick(2);Check(f.Minute==363,"same simulation time rate");
    session.Execute("pause");double minute=f.Minute;session.Tick(3);Check(f.Minute==minute,"pause blocks time");session.Execute("pause");
    int day=f.Day;session.Move(4,4);session.Execute("sleep");Check(f.Day==day+1&&p.Energy==100,"sleep resets day and energy");
    session.Save();var restored=Farm.Load(session.SavePath);Check(restored.Day==f.Day&&restored.Version==1&&restored.Plots.Count==24,"legacy Farm.Load reads new save");
    string old=JsonSerializer.Serialize(f);using var doc=JsonDocument.Parse(old);var legacy=new Dictionary<string,JsonElement>();foreach(var property in doc.RootElement.EnumerateObject())if(property.Name!="Effects"&&property.Name!="EffectSequence")legacy[property.Name]=property.Value;
    string legacyPath=Path.Combine(directory,"pre-animation-v1.json");File.WriteAllText(legacyPath,JsonSerializer.Serialize(legacy));Check(Farm.Load(legacyPath).Effects.Count==0,"older v1 without effects loads");
    File.WriteAllText(Path.Combine(directory,"invalid.json"),"{\"Version\":2}");bool refused=false;try{Farm.Load(Path.Combine(directory,"invalid.json"));}catch(InvalidDataException){refused=true;}Check(refused,"invalid save refused");
}
// Compile/link the unmodified protocol and exercise four authoritative clients, without cloud relay.
using(var server=new RoomServer(new Farm())){
    var clients=new List<RoomClient>();var secrets=new List<string>();
    try{
        for(int i=0;i<4;i++){secrets.Add(Guid.NewGuid().ToString("N"));var c=new RoomClient(server.Url,server.Token);clients.Add(c);var reply=await c.Send(new Request{Action="join",Session=secrets[i],Name="test"+i});Check(reply.Farm!=null,"legacy protocol join"+i);}
        using var fifth=new RoomClient(server.Url,server.Token);Check((await fifth.Send(new Request{Action="join",Session=Guid.NewGuid().ToString("N")})).Farm==null,"four-player cap");
        using var bad=new RoomClient(server.Url,new string('0',48));bool rejected=false;try{await bad.Send(new Request{Action="sync",Session=secrets[0]});}catch(IOException){rejected=true;}Check(rejected,"invalid bearer rejected");
        using var adapter=new LegacyRoomAdapter(server.Url,server.Token,secrets[0]);
        Check((await adapter.Join("adapter")).Farm!=null,"network adapter reconnect");
        string command=Guid.NewGuid().ToString("N");
        var first=await adapter.Command("water",6,6,0,"carrot",command);
        var duplicate=await adapter.Command("water",6,6,0,"carrot",command);
        Check(first.Farm?.Find(secrets[0])?.Energy==95&&duplicate.Farm?.Find(secrets[0])?.Energy==95,"command UUID retry is idempotent");
        Check((await adapter.Sync(6,6)).Farm?.Plots[0].Moisture==95,"network adapter authoritative moisture");
        var sync=await clients[0].Send(new Request{Session=secrets[0],Action="sync"});Check(sync.Farm?.Players.Count==4,"authoritative state has all players");
    }finally{foreach(var client in clients)client.Dispose();}
}
passed.AddRange(await PersistenceChecks.Run(Path.GetDirectoryName(output)!));
passed.AddRange(await ApplicationChecks.Run());
passed.AddRange(await NetworkChecks.Run());
passed.AddRange(await ArchitectureChecks.Run(Path.GetDirectoryName(output)!));
File.WriteAllText(output,JsonSerializer.Serialize(new{passed=passed.Count,checks=passed,scope="Core/save regressions and legacy local protocol. Godot online UI and internet co-op not migrated."},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("CORE PASS "+passed.Count);
