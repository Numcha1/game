using System.Text.Json;

namespace SuanPleng;
static class Program {
    [STAThread]
    static void Main(string[] args) {
        try {
            if(args.Contains("--self-test") || args.Contains("--internet-test")) { Checks.Run(args.Contains("--internet-test")).GetAwaiter().GetResult();return; }
            ApplicationConfiguration.Initialize();
            if(args.Contains("--render-test")) {
                string data=Path.Combine(AppContext.BaseDirectory,"render-data");using var form=new GameForm(data);
                using(var image=form.RenderForTest())image.Save(Path.Combine(AppContext.BaseDirectory,"menu.png"));
                var farm=new Farm();var p=farm.Join(Guid.NewGuid().ToString("N"),"ชาวสวน");farm.Owner=p.Id;
                farm.Plots[0]=new Plot{Crop="carrot",Growth=3,RipeDay=1};farm.Plots[1]=new Plot{Crop="tomato",Growth=2};
                form.TestWorld(farm);using(var image=form.RenderForTest())image.Save(Path.Combine(AppContext.BaseDirectory,"farm.png"));
                form.AnimationSmoke(AppContext.BaseDirectory);
                farm.Weather="ฝนตก";using(var image=form.RenderForTest())image.Save(Path.Combine(AppContext.BaseDirectory,"rain.png"));
                farm.Minute=1180;using(var image=form.RenderForTest())image.Save(Path.Combine(AppContext.BaseDirectory,"evening.png"));return;
            }
            Application.Run(new GameForm());
        } catch(Exception e) {
            if(args.Length>0){Console.Error.WriteLine(e.ToString());Environment.ExitCode=1;}
            else MessageBox.Show("เปิดเกมไม่ได้: "+e.Message,"สวนเพลง");
        }
    }
}
static class Checks {
    static int count;
    static void Assert(bool value,string label){if(!value)throw new Exception("FAIL: "+label);count++;}
    public static async Task Run(bool internet) {
        string session=Guid.NewGuid().ToString("N");var f=new Farm();var p=f.Join(session,"เจ้าของ");p.X=8;p.Y=6;
        Assert(f.Action(p,"plant",0,"carrot").StartsWith("ปลูก"),"plant near plot");Assert(f.Coins==152,"deduct cost");
        Assert(f.Action(p,"plant",0,"carrot").Contains("มีต้น"),"cannot plant twice");
        f.Plots[0].Moisture=0;f.NextDay();Assert(f.Plots[0].Growth==0,"dry crop no growth");
        p.X=8;p.Y=6;f.Action(p,"water",0,"carrot");f.Weather="แดดอ่อน";f.NextDay();Assert(f.Plots[0].Growth==1,"watered crop grows");
        f.Plots[0].Moisture=50;f.Weather="แดดอ่อน";f.NextDay();f.Plots[0].Moisture=50;f.Weather="แดดอ่อน";f.NextDay();Assert(f.Plots[0].RipeDay==f.Day,"harvest window begins");
        p.X=8;p.Y=6;f.Action(p,"harvest",0,"carrot");int harvested=p.Bag["carrot"];Assert(harvested>0,"bag receives crop");
        f.Action(p,"harvest",0,"carrot");Assert(p.Bag["carrot"]==harvested,"duplicate harvest rejected");
        p.X=20;p.Y=8;int coins=f.Coins;f.Action(p,"sell",0,"carrot");Assert(f.Coins>coins&&p.Bag.Count==0,"sell bag");
        var seasonFarm=new Farm{Day=14};seasonFarm.Plots[0]=new Plot{Crop="berry",Growth=1,Moisture=50};seasonFarm.NextDay();Assert(seasonFarm.Season==1&&seasonFarm.Plots[0].Crop=="","season kills incompatible crop");
        var stale=new Farm{Day=5};stale.Plots[0]=new Plot{Crop="berry",Growth=6,RipeDay=2,Moisture=50};stale.NextDay();Assert(stale.Plots[0].Crop=="","late crop spoils");
        var storm=new Farm{Weather="พายุ"};storm.Plots[0]=new Plot{Crop="carrot",Moisture=30};storm.Plots[1]=new Plot{Crop="carrot",Moisture=30,Stake=true};storm.NextDay();Assert(storm.Plots[0].Quality<storm.Plots[1].Quality,"stakes protect storm");
        var sickness=new Farm{Weather="ฝนตก"};sickness.Plots[0]=new Plot{Crop="carrot",Moisture=80};sickness.NextDay();Assert(sickness.Plots[0].Disease,"saturated soil disease");
        var rotation=new Farm();var rot=rotation.Join(Guid.NewGuid().ToString("N"),"คนปลูก");rot.X=8;rot.Y=6;rotation.Plots[0].LastCrop="carrot";rotation.Action(rot,"plant",0,"carrot");Assert(rotation.Plots[0].Fertility==65,"rotation penalty");
        var coop=new Farm();string s1=Guid.NewGuid().ToString("N"),s2=Guid.NewGuid().ToString("N");var a=coop.Join(s1,"A");var b=coop.Join(s2,"B");a.X=b.X=4;a.Y=b.Y=4;coop.Action(a,"sleep",-1,"");Assert(coop.Day==1,"sleep waits for peers");coop.Action(b,"sleep",-1,"");Assert(coop.Day==2,"all sleep advances day");
        Assert(coop.Action(b,"pause",-1,"").Contains("เจ้าของ"),"guest cannot pause");
        coop.Coins=0;coop.Rescue(a);Assert(coop.Coins==18,"rescue cannot deadlock");
        string temp=Path.Combine(Path.GetTempPath(),"SuanPleng-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        string path=Path.Combine(temp,"farm.json");coop.Save(path);var loaded=Farm.Load(path);Assert(loaded.Day==coop.Day&&loaded.Players.All(v=>v.LastSeen==0),"save/load stops offline clock");
        var legacy=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!.AsObject();legacy.Remove("Effects");legacy.Remove("EffectSequence");File.WriteAllText(path,legacy.ToJsonString());
        loaded=Farm.Load(path);Assert(loaded.Day==coop.Day && loaded.Effects.Count==0 && loaded.EffectSequence==0,"legacy save without animation fields loads");
        File.WriteAllText(path,"{broken");try{Farm.Load(path);Assert(false,"corrupt save refused");}catch(JsonException){count++;}
        using var server=new RoomServer(new Farm());using var c1=new RoomClient(server.Url,server.Token);using var c2=new RoomClient(server.Url,server.Token);
        await CheckRoom(c1,c2,s1,s2);
        var raceFarm=new Farm();var r1=raceFarm.Join(s1,"A");var r2=raceFarm.Join(s2,"B");r1.X=r2.X=8;r1.Y=r2.Y=6;
        raceFarm.Plots[0]=new Plot{Crop="carrot",Growth=3,RipeDay=1};
        using(var raceServer=new RoomServer(raceFarm))using(var rc1=new RoomClient(raceServer.Url,raceServer.Token))using(var rc2=new RoomClient(raceServer.Url,raceServer.Token)) {
            var results=await Task.WhenAll(rc1.Send(new Request{Session=s1,Action="harvest",Index=0,X=8,Y=6,Command=Guid.NewGuid().ToString("N")}),rc2.Send(new Request{Session=s2,Action="harvest",Index=0,X=8,Y=6,Command=Guid.NewGuid().ToString("N")}));
            Assert(raceFarm.Players.Sum(v=>v.Bag.GetValueOrDefault("carrot"))==3,"concurrent harvest granted once");
            Assert(results.Count(v=>v.Message.Contains("ใส่กระเป๋า"))==1,"one harvest winner");
            Assert(raceFarm.Effects.Count==1 && raceFarm.Effects[0].Kind=="harvest","one confirmed harvest effect");
            var fx=await rc2.Send(new Request{Session=s2,Action="sync",X=8,Y=6});
            Assert(fx.Farm!.Effects.Count==1&&fx.Farm.Effects[0].Actor==raceFarm.Effects[0].Actor,"peer receives actor effect");
            int countBefore=raceFarm.Effects.Count;
            await rc2.Send(new Request{Session=s2,Action="harvest",Index=0,X=8,Y=6,Command=Guid.NewGuid().ToString("N")});
            Assert(raceFarm.Effects.Count==countBefore,"failed action has no success animation");
            var third=await rc1.Send(new Request{Session=Guid.NewGuid().ToString("N"),Action="join",Name="C"});
            var fourth=await rc1.Send(new Request{Session=Guid.NewGuid().ToString("N"),Action="join",Name="D"});
            var fifth=await rc1.Send(new Request{Session=Guid.NewGuid().ToString("N"),Action="join",Name="E"});
            Assert(fourth.Farm!=null && fifth.Farm==null && fifth.Message.Contains("เต็ม"),"four player capacity enforced");
        }
        using(var invalid=new RoomClient(server.Url,"wrong")){try{await invalid.Send(new Request{Session=s1});Assert(false,"unauthorized room access");}catch(IOException){count++;}}
        try{InternetTunnel.Parse("https://evil.example#"+server.Token);Assert(false,"invite host validation");}catch(InvalidOperationException){count++;}
        if(internet){using var tunnel=new InternetTunnel();string address=await tunnel.Start(server.Port);using var remote1=new RoomClient(address,server.Token);using var remote2=new RoomClient(address,server.Token);bool ready=false;for(int attempt=0;attempt<12&&!ready;attempt++){try{await remote1.Send(new Request{Session=s1,Action="join",Name="A"});ready=true;}catch(Exception e)when(e is IOException or HttpRequestException or TaskCanceledException){await Task.Delay(1500);}}Assert(ready,"public tunnel reachable");await CheckRoom(remote1,remote2,s1,s2);}
        string result=JsonSerializer.Serialize(new{passed=count,internet,utc=DateTime.UtcNow});Console.WriteLine(result);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,internet?"internet-test.json":"self-test.json"),result);
    }
    static async Task CheckRoom(RoomClient c1,RoomClient c2,string s1,string s2) {
        var joined=await c1.Send(new Request{Session=s1,Action="join",Name="A"});await c2.Send(new Request{Session=s2,Action="join",Name="B"});
        var view=await c1.Send(new Request{Session=s1,Action="sync"});Assert(view.Farm?.Players.Count==2,"two clients share room");
        Assert(!JsonSerializer.Serialize(view).Contains(s1),"resume credential absent from snapshots");
        var action=new Request{Session=s1,Action="rescue",Command=Guid.NewGuid().ToString("N")};
        var first=await c1.Send(action);var again=await c1.Send(action);Assert(first.Farm!.Coins==again.Farm!.Coins&&first.Message==again.Message,"command retry idempotent");
        Assert(first.Farm.EffectSequence==again.Farm.EffectSequence,"retry does not replay effects");
        var paused=await c2.Send(new Request{Session=s2,Action="pause",Command=Guid.NewGuid().ToString("N")});Assert(!paused.Farm!.Paused,"guest authority checked over network");
        var ownerPause=await c1.Send(new Request{Session=s1,Action="pause",Command=Guid.NewGuid().ToString("N")});
        var peer=await c2.Send(new Request{Session=s2,Action="sync"});Assert(peer.Farm!.Paused==ownerPause.Farm!.Paused,"authoritative world shared");
        if(ownerPause.Farm.Paused)await c1.Send(new Request{Session=s1,Action="pause",Command=Guid.NewGuid().ToString("N")});
    }
}
