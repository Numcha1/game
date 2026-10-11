using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Infrastructure.Persistence;
using SuanPleng;
using System.Text.Json.Nodes;

static class NpcChecks
{
    public static async Task<List<string>> Run(string reports)
    {
        var checks=new List<string>();void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);}
        var mira=NpcRegistry.Find("mira")!;var rowan=NpcRegistry.Find("rowan")!;
        Check(NpcRegistry.All.Count==12 && NpcRegistry.Active.Count()==2 && NpcRegistry.Find("theo")==null,"roster distinguishes two functional NPCs from ten planned definitions");
        foreach(var npc in NpcRegistry.Active) {
            Check(NpcSchedules.Resolve(npc,400,"แดดอ่อน",0).Region==npc.Home,"morning home schedule "+npc.Id);
            Check(NpcSchedules.Resolve(npc,600,"แดดอ่อน",0).Region==npc.Workplace,"day work schedule "+npc.Id);
            Check(NpcSchedules.Resolve(npc,750,"แดดอ่อน",0).Region==NpcRegistry.Village,"fair lunch in square "+npc.Id);
            Check(NpcSchedules.Resolve(npc,750,"พายุ",0).Region==npc.Workplace,"storm shelters indoors "+npc.Id);
            Check(NpcSchedules.Resolve(npc,1150,"แดดอ่อน",0).Region==npc.Home,"evening home schedule "+npc.Id);
        }
        var map=BuildingRegistry.Village;
        var path=NpcNavigation.FindPath(map,new(4,5.5),new(21,5.5));
        Check(path.Count>0 && path.All(map.Walkable),"bounded grid route stays within village navigation");
        Check(NpcNavigation.FindPath(map,new(double.NaN,6),new(12,10)).Count==0,"invalid path rejected");
        var farm=new Farm {Minute=479};var village=new VillageState();var sim=new NpcSimulation();sim.Initialize(village,farm);
        farm.Minute=480;var positions=village.Npcs.ToDictionary(p=>p.Key,p=>p.Value.Position);
        sim.Advance(village,farm,NpcRegistry.Village,.1);
        Check(village.Npcs.All(p=>p.Value.Position.Distance(positions[p.Key])<=.1651),"schedule change advances by walking instead of visible teleport");
        farm.Paused=true;var before=village.Npcs["mira"].Position;sim.Advance(village,farm,NpcRegistry.Village,1);
        Check(village.Npcs["mira"].Position==before,"pause stops NPC navigation");farm.Paused=false;
        for(int i=0;i<600;i++)sim.Advance(village,farm,NpcRegistry.Village,.1);
        Check(village.Npcs["mira"].Region==mira.Workplace && village.Npcs["rowan"].Region==rowan.Workplace,"two NPCs leave home walk outdoors and enter workplaces");
        farm.Minute=1150;for(int i=0;i<600;i++)sim.Advance(village,farm,NpcRegistry.Village,.1);
        Check(village.Npcs["mira"].Region==mira.Home && village.Npcs["rowan"].Region==rowan.Home,"two NPCs return through real home entrances");
        farm.Minute=600;sim.Advance(village,farm,"",.1);
        Check(village.Npcs["mira"].Region==mira.Workplace,"unloaded regions resolve schedule without scene nodes");
        Check(!NpcDialogue.Text("missing").Contains("missing"),"missing dialogue has safe Thai fallback");
        string profile=Path.Combine(reports,"npc-profile-"+Guid.NewGuid().ToString("N"));Guid purchase=Guid.NewGuid(),carpentry=Guid.NewGuid();
        await using(var game=new GameSession(new SaveService(profile))) {
            Check(game.NpcAccessError(null!)!="","null NPC command identity is rejected without throwing");
            game.State.Minute=600;game.State.Coins=500;
            Check(!game.BuySeeds("carrot",1,purchase).Changed,"remote seed purchase rejected");
            game.Move(23,8);Check(game.EnterBuilding(map.Id).Changed,"village entry retains native exterior anchor");
            Check(game.Npcs().Count==2,"village initializes exactly two persistent instances");
            var shop=BuildingRegistry.Find(mira.Workplace)!;game.Move(shop.Entrance.X,shop.Entrance.Y);
            Check(game.EnterBuilding(shop.Id).Changed && Farm.Walkable(game.Player.X,game.Player.Y),"nested shop entry does not store indoor coordinates in legacy player");
            Check(!game.BuySeeds("carrot",1,purchase).Changed,"distant shop interaction rejected");game.Move(12,8);
            int coins=game.State.Coins,cost=Farm.Crops["carrot"].Cost;
            Check(!game.BuySeeds("unknown",1,purchase).Changed && !game.BuySeeds("carrot",0,purchase).Changed && !game.BuySeeds("carrot",1,Guid.Empty).Changed,"unknown seed quantity and UUID rejected");
            Check(game.BuySeeds("carrot",2,purchase).Changed && game.State.Coins==coins-cost*2 && game.SeedCount("carrot")==2,"merchant charges exact original seed cost");
            Check(!game.BuySeeds("carrot",2,purchase).Changed && game.SeedCount("carrot")==2,"purchase UUID cannot double-spend");
            game.Dialogue("mira");int friendship=game.Buildings.Village.Npcs["mira"].Friendship[game.Player.Id];game.Dialogue("mira");
            Check(friendship==10 && game.Buildings.Village.Npcs["mira"].Friendship[game.Player.Id]==10,"conversation reward limited to once per day");
            game.Move(12,14);Check(game.ExitBuilding().Changed && game.ActiveBuildingId==map.Id,"shop exit returns to its village door");
            var workshop=BuildingRegistry.Find(rowan.Workplace)!;game.Move(workshop.Entrance.X,workshop.Entrance.Y);game.EnterBuilding(workshop.Id);game.Move(12,8);
            game.State.Coins=299;Check(!game.RequestCarpentry(carpentry).Changed && !game.State.Greenhouse,"carpentry preserves original insufficient-funds rule");game.State.Coins=500;
            Check(game.RequestCarpentry(carpentry).Changed && game.State.Coins==200 && game.State.Greenhouse,"Rowan installs actual original greenhouse for 300 coins");
            Check(!game.RequestCarpentry(carpentry).Changed && game.State.Coins==200,"carpentry UUID cannot duplicate construction");
            game.ExitBuilding(true);game.Move(8,6);coins=game.State.Coins;
            Check(game.Execute(new("plant",0,"carrot",Guid.NewGuid())).Changed && game.State.Coins==coins && game.SeedCount("carrot")==1,"prepaid seed used once by original planting rule without double charge");
            Check(!game.Execute(new("plant",0,"carrot",Guid.NewGuid())).Changed && game.SeedCount("carrot")==1 && game.State.Coins==coins,"failed planting cannot consume prepaid seed or refund extra money");
            game.Move(23,8);game.EnterBuilding(map.Id);game.SaveAsync().GetAwaiter().GetResult();
        }
        await using(var game=new GameSession(new SaveService(profile))) {
            Check(game.ActiveBuildingId==map.Id && game.Npcs().Count==2 && game.SeedCount("carrot")==1,"atomic reload restores village NPCs prepaid seeds and player region");
            var shop=BuildingRegistry.Find(mira.Workplace)!;game.Move(shop.Entrance.X,shop.Entrance.Y);game.EnterBuilding(shop.Id);game.Move(12,8);
            Check(!game.BuySeeds("carrot",2,purchase).Changed,"recent successful purchase UUID survives save and restart");
            Check(game.Buildings.Village.Npcs["mira"].Friendship[game.Player.Id]==10,"friendship survives restart");
            game.State.Minute=750;game.State.Weather="พายุ";game.Tick(.1);
            Check(game.Npcs().All(n=>n.Activity=="ทำงาน"),"restored simulation follows storm schedule");
            game.State.Minute=1259.9;int day=game.State.Day;game.Tick(.2);
            Check(game.State.Day==day+1 && game.Npcs().All(n=>BuildingRegistry.Find(n.Region)!=null),"native day rollover leaves valid NPC region state");
            game.SaveAsync().GetAwaiter().GetResult();
        }
        var saved=JsonNode.Parse(File.ReadAllText(Path.Combine(profile,"farm.json")))!;
        foreach(string fault in new[]{"unknown-npc","position","friendship","null-village"}) {
            var bad=saved.DeepClone();var v=bad["Buildings"]!["Village"]!;
            if(fault=="unknown-npc")v["Npcs"]!["intruder"]=v["Npcs"]!["mira"]!.DeepClone();
            if(fault=="position")v["Npcs"]!["mira"]!["X"]=999;
            if(fault=="friendship")v["Npcs"]!["mira"]!["Friendship"]!["unknown-player"]=1001;
            if(fault=="null-village")bad["Buildings"]!["Village"]=null;
            bool refused=false;try{SaveService.Deserialize(System.Text.Encoding.UTF8.GetBytes(bad.ToJsonString()));}catch(InvalidDataException){refused=true;}
            Check(refused,"strict NPC save validation rejects "+fault);
        }
        saved["Buildings"]!.AsObject().Remove("Village");
        bool missingRefused=false;try{SaveService.Deserialize(System.Text.Encoding.UTF8.GetBytes(saved.ToJsonString()));}catch(InvalidDataException){missingRefused=true;}
        Check(missingRefused,"used village cannot silently reset NPC state when its save section is missing");
        saved["Buildings"]!["Interiors"]=new JsonObject {[BuildingRegistry.Farmhouse.Id]=System.Text.Json.JsonSerializer.SerializeToNode(new InteriorState())};
        saved["Buildings"]!["Locations"]=new JsonObject();
        Check(SaveService.Deserialize(System.Text.Encoding.UTF8.GetBytes(saved.ToJsonString())).Version==1,"actual pre-NPC farmhouse version-two save loads without village member");
        string metadataProfile=Path.Combine(reports,"npc-profile-"+Guid.NewGuid().ToString("N"));Guid applied=Guid.NewGuid();
        await using(var game=new GameSession(new SaveService(metadataProfile))) {
            game.Buildings.Village.AppliedCommands.Add(applied);game.SaveAsync().GetAwaiter().GetResult();
        }
        await using(var game=new GameSession(new SaveService(metadataProfile)))
            Check(game.Buildings.Village.AppliedCommands.SequenceEqual(new[]{applied}),"metadata-only NPC replay ledger survives atomic save without reverting to v1");
        return checks;
    }
}
