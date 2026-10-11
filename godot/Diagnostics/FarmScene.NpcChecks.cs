using Godot;
using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Infrastructure.Persistence;
using System.Text.Json;

namespace SeasonsTogether;

public partial class FarmScene
{
    static bool npcHarnessRunning;
    async Task RunNpcChecks()
    {
        if(npcHarnessRunning)return;npcHarnessRunning=true;
        var checks=new List<string>();void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);checks.Add(label);}
        string reports=ProjectSettings.GlobalizePath("res://reports");
        async Task DrawShot(string name) {
            if(DisplayServer.GetName()=="headless")return;
            await NextDraw();using var texture=GetViewport().GetTexture();using var shot=texture.GetImage();shot.SavePng(Path.Combine(reports,name+".png"));
        }
        async Task Enter(string id) {RequestBuildingEntry(id);await TransitionCompletion;await NextFrame(SceneTree.SignalName.ProcessFrame);Check(Session.ActiveBuildingId==id && InteriorVisible && !Farmer.InputBlocked,"real transition into "+id);}
        async Task Exit() {Session.Move(12,14);RequestBuildingExit();await TransitionCompletion;await NextFrame(SceneTree.SignalName.ProcessFrame);}
        try {
            if(arguments.Contains("--furniture-checks")){await RunFurnitureChecks();return;}
            if(arguments.Contains("--inventory-checks")){await RunInventoryChecks();return;}
            if(arguments.Contains("--bake-presentation")){PresentationAssetBake.WriteNpcAtlas();GD.Print("ASSET BAKE PASS");GetTree().Quit();return;}
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            Session.State.Minute=600;Session.State.Coins=500;Session.Move(23,8);
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(DoorPrompt.Contains("หมู่บ้าน"),"Thai village route prompt on original farm");
            DiagnosticInteriorSceneOverride="res://Scenes/Village/missing.tscn";RequestBuildingEntry(NpcRegistry.Village);await TransitionCompletion;
            Check(!Session.InsideBuilding && !Farmer.InputBlocked,"missing village scene preserves farm and controls");DiagnosticInteriorSceneOverride=null;
            await Enter(NpcRegistry.Village);
            Check(Session.Npcs().Count==2 && interior!.GetNode<Node2D>("Actors").GetChildren().OfType<NpcActor>().Count()==2,"two original NPC actor nodes created once");
            string originalWeather=Session.State.Weather;
            Session.State.Weather="ฝนตก";
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(weather.Visible && weather.IsProcessing() && !accents.PondRipplesEnabled && accents.VisibleRipples==0,"village uses outdoor rain without farm pond rings on dry land");
            Session.State.Weather=originalWeather;
            Check(NpcAssets.Characters.GetWidth()==320 && NpcAssets.Characters.GetHeight()==224 && NpcAssets.Frames("mira").GetFrameCount("up")==4 && ReferenceEquals(NpcAssets.Frames("mira"),NpcAssets.Frames("mira")),"shared directional NPC sprite atlas and cached frames");
            using(var pixels=NpcAssets.Characters.GetImage())Check(pixels.GetPixel(0,0).A<.01f,"original sprite sheet preserves transparency");
            if(arguments.Contains("--composition-checks")){await RunCompositionChecks();return;}
            if(arguments.Contains("--indoor-layout-checks")){await RunIndoorLayoutChecks();return;}
            if(arguments.Contains("--npc-assets-only")) {
                Session.Move(12,14);RequestBuildingExit();await TransitionCompletion;await NextFrame(SceneTree.SignalName.ProcessFrame);
                await Session.SaveAsync();finalCheckpoint=true;
                for(int i=0;i<40;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
                GC.Collect();GC.WaitForPendingFinalizers();GD.Print("NPC ASSET PASS");GetTree().CallDeferred(SceneTree.MethodName.Quit,0);return;
            }
            Session.State.Weather="แดดอ่อน";Session.State.Minute=750;
            bool sawMira=false,sawRowan=false;double maxStep=0;
            var previous=Session.Npcs().ToDictionary(n=>n.Id,n=>n);
            for(int i=0;i<800;i++) {
                await NextFrame(SceneTree.SignalName.ProcessFrame);
                foreach(var npc in Session.Npcs()) {
                    var old=previous[npc.Id];
                    if(old.Region==NpcRegistry.Village && npc.Region==old.Region)maxStep=Math.Max(maxStep,new WorldPoint(npc.X,npc.Y).Distance(new(old.X,old.Y)));
                    if(npc.Region==NpcRegistry.Village){if(npc.Id=="mira")sawMira=true;else sawRowan=true;}
                    previous[npc.Id]=npc;
                }
            }
            Check(sawMira && sawRowan && maxStep>0 && maxStep<.5,"both visibly leave workplaces and walk to lunch without nearby teleport");
            Check(Session.Npcs().All(n=>n.Region==NpcRegistry.Village),"both arrive at actual village square");
            Session.Move(12,11);await DrawShot("village-square");
            Session.TryNpc("mira",out var mira);Session.Move(mira.X,mira.Y+1);await NextFrame(SceneTree.SignalName.ProcessFrame);
            using(var input=new InputEventKey{PhysicalKeycode=Key.F,Pressed=true})_UnhandledInput(input);
            Check(Hud.ModalVisible && Farmer.InputBlocked && Hud.NpcDialogueText.Contains("มิรา"),"F opens readable Thai Mira dialogue and blocks movement");
            Hud.FindChildren("NpcChoice-guide","Button",true,false).OfType<Button>().First().EmitSignal(Button.SignalName.Pressed);
            Check(Hud.NpcDialogueText.Contains("รดน้ำ"),"real dialogue choice switches to original farming guidance");
            await DrawShot("mira-dialogue");Hud.CloseModal();
            Session.TryNpc("rowan",out var rowan);Session.Move(rowan.X,rowan.Y+1);Hud.OpenNpc("rowan");
            Check(Hud.ModalVisible && Hud.NpcDialogueText.Contains("โรวัน"),"Rowan has own Thai dialogue and portrait");Hud.CloseModal();
            Session.State.Minute=780;
            for(int i=0;i<1000;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(Session.Npcs().All(n=>n.Region==NpcRegistry.Find(n.Id)!.Workplace),"NPCs visibly return through workshop and seed shop doors");
            var shop=BuildingRegistry.Find("village.seed-shop")!;Session.Move(shop.Entrance.X,shop.Entrance.Y);await Enter(shop.Id);
            Check(!weather.Visible && !weather.IsProcessing(),"shop roof suppresses rain effects");
            Session.TryNpc("mira",out mira);Session.Move(mira.X,mira.Y+1);Hud.OpenNpc("mira");
            int coins=Session.Read.World.Coins,count=Session.SeedCount("carrot");
            var buy=Hud.FindChildren("BuySeed-carrot","Button",true,false).OfType<Button>().First();buy.EmitSignal(Button.SignalName.Pressed);buy.EmitSignal(Button.SignalName.Pressed);
            Check(Session.SeedCount("carrot")==count+1 && Session.Read.World.Coins==coins-SuanPleng.Farm.Crops["carrot"].Cost,"real shop button delivers one prepaid seed for exact authoritative price");
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            Hud.FindChildren("*","ScrollContainer",true,false).OfType<ScrollContainer>().First().ScrollVertical=250;
            for(int settle=0;settle<20;settle++)await NextFrame(SceneTree.SignalName.ProcessFrame);
            await DrawShot("mira-seed-shop");Hud.CloseModal();
            await Exit();Check(Session.ActiveBuildingId==NpcRegistry.Village,"seed shop exit returns to village not native farm");
            var work=BuildingRegistry.Find("village.workshop")!;Session.Move(work.Entrance.X,work.Entrance.Y);await Enter(work.Id);
            Session.TryNpc("rowan",out rowan);Session.Move(rowan.X,rowan.Y+1);Hud.OpenNpc("rowan");coins=Session.Read.World.Coins;
            var build=Hud.FindChildren("NpcCarpentry","Button",true,false).OfType<Button>().First();build.EmitSignal(Button.SignalName.Pressed);build.EmitSignal(Button.SignalName.Pressed);
            Check(Session.Read.World.Greenhouse && Session.Read.World.Coins==coins-300,"Rowan button installs actual greenhouse once");
            for(int settle=0;settle<20;settle++)await NextFrame(SceneTree.SignalName.ProcessFrame);
            await DrawShot("rowan-workshop");Hud.CloseModal();await Exit();
            Session.State.Minute=1150;
            for(int i=0;i<1000;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(Session.Npcs().All(n=>n.Region==NpcRegistry.Find(n.Id)!.Home),"evening routines enter both real home doors");
            foreach(var npc in NpcRegistry.Active) {
                var home=BuildingRegistry.Find(npc.Home)!;Session.Move(home.Entrance.X,home.Entrance.Y);await Enter(home.Id);
                Session.TryNpc(npc.Id,out var resident);Session.Move(resident.X,resident.Y+1);Hud.OpenNpc(npc.Id);
                Check(Hud.ModalVisible && Hud.NpcDialogueText.Contains("พัก"),"home resident dialogue for "+npc.Id);Hud.CloseModal();await DrawShot(npc.Id+"-home");await Exit();
            }
            var frames=new List<double>();long last=System.Diagnostics.Stopwatch.GetTimestamp();
            if(DisplayServer.GetName()!="headless")for(int i=0;i<300;i++) {
                await NextFrame(SceneTree.SignalName.ProcessFrame);long now=System.Diagnostics.Stopwatch.GetTimestamp();frames.Add((now-last)*1000.0/System.Diagnostics.Stopwatch.Frequency);last=now;
            }
            object? measurement=null;
            if(frames.Count>0){var ordered=frames.Order().ToArray();measurement=new{frames=ordered.Length,meanMs=ordered.Average(),p95Ms=ordered[(int)(ordered.Length*.95)],fps=1000/ordered.Average(),privateBytes=System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64,managedBytes=GC.GetTotalMemory(false)};Check(ordered.Average()<=17 && ordered[(int)(ordered.Length*.95)]<=18,"actual rendered village meets frame-time target");}
            await Session.SaveAsync();finalCheckpoint=true;
            Check(SaveService.Deserialize(File.ReadAllBytes(Session.SavePath)).Version==1,"actual village save retains valid native Farm payload");
            var savedNpcs=Session.Npcs();
            long baseline=(long)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
            for(int restart=0;restart<3;restart++) {
                string copy=Path.Combine(reports,"npc-profile-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(copy);
                File.Copy(Path.Combine(DataDirectory,"farm.json"),Path.Combine(copy,"farm.json"));File.Copy(Path.Combine(DataDirectory,"player.txt"),Path.Combine(copy,"player.txt"));
                var restored=ResourceLoader.Load<PackedScene>("res://Scenes/Farm.tscn").Instantiate<FarmScene>();restored.BuildingDiagnosticDirectory=copy;GetTree().Root.AddChild(restored);
                await restored.TransitionCompletion;await NextFrame(SceneTree.SignalName.ProcessFrame);
                bool correct=restored.InteriorVisible && restored.Session.ActiveBuildingId==NpcRegistry.Village && restored.Session.Npcs().Count==2 && restored.Session.SeedCount("carrot")==Session.SeedCount("carrot") && restored.Session.Read.World.Greenhouse && savedNpcs.All(saved=>restored.Session.TryNpc(saved.Id,out var npc) && npc.Region==saved.Region && new WorldPoint(npc.X,npc.Y).Distance(new(saved.X,saved.Y))<.05);
                var restoredSession=restored.Session;GetTree().Root.RemoveChild(restored);Task closed=restored.shutdown;restored.QueueFree();await NextFrame(SceneTree.SignalName.ProcessFrame);await closed.WaitAsync(TimeSpan.FromSeconds(5));await NextFrame(SceneTree.SignalName.ProcessFrame);Farmer.Camera.MakeCurrent();
                Check(correct,"actual scene reload restores NPC village seeds and service state "+restart);
                Check(!GodotObject.IsInstanceValid(restored) && restoredSession.ObserverCount==0 && (long)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)==baseline,"NPC scene teardown returns nodes and observers to baseline "+restart);
            }
            int day=Session.Read.World.Day;Session.State.Minute=1259.9;Session.Tick(.2);await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(Session.Read.World.Day==day+1 && Session.Npcs().All(n=>BuildingRegistry.Find(n.Region)!=null),"actual scene remains valid through original day rollover");
            await Exit();Check(!Session.InsideBuilding && !Farmer.InputBlocked,"village exit returns to playable original farm");
            await Session.SaveAsync();finalCheckpoint=true;
            File.WriteAllText(Path.Combine(reports,"npcs-"+DisplayServer.GetName()+".json"),JsonSerializer.Serialize(new{passed=checks.Count,checks,measurement,scope="Actual isolated Godot NPC schedules doors dialogue shop carpenter homes and frame tests; no internet certification"},new JsonSerializerOptions{WriteIndented=true}));
            for(int i=0;i<40;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
            GC.Collect();GC.WaitForPendingFinalizers();
            GD.Print("NPC PASS "+checks.Count);GetTree().CallDeferred(SceneTree.MethodName.Quit,0);
        }
        catch(Exception e){GD.PushError("NPC FAIL "+e);GetTree().Quit(1);}
    }
}
