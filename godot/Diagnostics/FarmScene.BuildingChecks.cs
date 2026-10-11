using Godot;
using SeasonsTogether.Core.Domain;
using System.Text.Json;

namespace SeasonsTogether;

public partial class FarmScene
{
    static bool buildingHarnessRunning;
    object? indoorMeasurement;
    async Task RunBuildingChecks()
    {
        if(buildingHarnessRunning)return;buildingHarnessRunning=true;
        var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
        try {
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(!Session.InsideBuilding && !InteriorVisible,"initial farm has no interior");
            RequestBuildingEntry("unknown");Check(!IsBusy && !Session.InsideBuilding,"unknown building rejected before transition");
            Session.Move(10,10);RequestBuildingEntry(BuildingRegistry.Farmhouse.Id);
            Check(!IsBusy && !Session.InsideBuilding,"remote entrance rejected");
            Session.Move(3.5,4.5);await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(DoorPrompt.Contains("F"),"nearby Thai door prompt");
            DiagnosticInteriorSceneOverride="res://Scenes/Interiors/missing.tscn";
            RequestBuildingEntry(BuildingRegistry.Farmhouse.Id);await TransitionCompletion;
            Check(!Session.InsideBuilding && !InteriorVisible && !Farmer.InputBlocked,"missing resource returns to interactive farm");DiagnosticInteriorSceneOverride=null;
            int energy=Session.Read.Player.Energy, coins=Session.Read.World.Coins;
            for(int round=0;round<5;round++) {
                double minute=Session.Read.World.Minute;
                RequestBuildingEntry(BuildingRegistry.Farmhouse.Id);
                var pending=TransitionCompletion;RequestBuildingEntry(BuildingRegistry.Farmhouse.Id);
                Check(ReferenceEquals(pending,TransitionCompletion) && Farmer.InputBlocked,"duplicate door request blocked "+round);
                await pending;
                var spawn=BuildingRegistry.Farmhouse.Spawn;
                Check(Session.InsideBuilding && InteriorVisible && Session.Position.Distance(spawn)<.05 && Farmer.GetParent()==interior!.GetNode<Node2D>("Actors"),"correct interior spawn and retained actor "+round);
                Check(!weather.Visible && !weather.IsProcessing() && !GetNode<TileMapLayer>("Ground").Visible,"outside weather and ground hidden indoors "+round);
                Check(Session.Read.World.Minute>=minute && Session.Read.Player.Energy==energy && Session.Read.World.Coins==coins,"time inventory and energy preserved "+round);
                Check(!Session.Move(4.5,5) && !Session.Move(7,3) && !Session.Move(-1,8) && !Session.Execute(new("plant",0,"carrot")).Changed,"bed back-wall collision and outdoor action isolation "+round);
                if(round==0 && DisplayServer.GetName()!="headless") {
                    await NextDraw();using var shot=GetViewport().GetTexture().GetImage();shot.SavePng(Path.Combine(ProjectSettings.GlobalizePath("res://reports"),"farmhouse-interior.png"));
                }
                if(round==0)await CheckFarmhouseFurniture(Check);
                Session.Move(12,14);RequestBuildingExit();await TransitionCompletion;
                Check(!Session.InsideBuilding && !InteriorVisible && Session.Position.Distance(BuildingRegistry.Farmhouse.Return)<.05 && weather.Visible && !Farmer.InputBlocked,"correct safe exterior return "+round);
            }
            Check(interiorLoader.CachedCount==1,"interior scene asset cached across repeated entry");
            await Session.SaveAsync();finalCheckpoint=true;
            string output=Path.Combine(ProjectSettings.GlobalizePath("res://reports"),"buildings-"+DisplayServer.GetName()+".json");
            await System.IO.File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=checks.Count,checks,indoorMeasurement,scope="Real Godot farmhouse entrance/exit/resource failure/actor/collision/storage/bed/restart tests in isolated profile"},new JsonSerializerOptions{WriteIndented=true}));
            GD.Print("BUILDINGS PASS "+checks.Count);GetTree().Quit();
        } catch(Exception e){if(sceneActive){GD.PushError("BUILDINGS FAIL "+e);GetTree().Quit(1);}}
    }
    async Task CheckFarmhouseFurniture(Action<bool,string> check)
    {
        var home=BuildingRegistry.Farmhouse;
        double start=Session.Position.X;
        Input.ParseInputEvent(new InputEventKey {PhysicalKeycode=Key.D,Pressed=true});
        for(int n=0;n<12;n++)await NextFrame(SceneTree.SignalName.PhysicsFrame);
        Input.ParseInputEvent(new InputEventKey {PhysicalKeycode=Key.D,Pressed=false});
        check(Session.Position.X>start && Farmer.Position==Farmer.Position.Round(),"actual indoor WASD movement and pixel-aligned actor");
        Session.Player.Bag["carrot"]=10;Session.Move(home.Chest.X,home.Chest.Y);
        InteractBuilding();check(Hud.StorageModalVisible && Farmer.InputBlocked,"F opens functional storage modal");
        var deposit=Hud.FindChild("StorageDeposit-carrot",true,false) as Button ?? throw new InvalidOperationException("deposit button missing");
        deposit.EmitSignal(Button.SignalName.Pressed);deposit.EmitSignal(Button.SignalName.Pressed);
        check(Session.Read.Storage()[0].Quantity==1 && Session.Read.Inventory()[0].Quantity==9,"storage UI deposits once despite duplicate signal");
        if(DisplayServer.GetName()!="headless") {
            await NextDraw();using var shot=GetViewport().GetTexture().GetImage();shot.SavePng(Path.Combine(ProjectSettings.GlobalizePath("res://reports"),"farmhouse-storage.png"));
        }
        Hud.CloseModal();Session.TransferStorage("carrot",9,true,Guid.NewGuid());var savedPosition=Session.Position;await Session.SaveAsync();
        string copy=Path.Combine(ProjectSettings.GlobalizePath("res://reports"),"building-profile-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(copy);
        System.IO.File.Copy(Path.Combine(DataDirectory,"farm.json"),Path.Combine(copy,"farm.json"));
        System.IO.File.Copy(Path.Combine(DataDirectory,"player.txt"),Path.Combine(copy,"player.txt"));
        var template=ResourceLoader.Load<PackedScene>("res://Scenes/Farm.tscn");
        var restarted=template.Instantiate<FarmScene>();restarted.BuildingDiagnosticDirectory=copy;GetTree().Root.AddChild(restarted);
        await restarted.TransitionCompletion;
        bool restoreOk=restarted.InteriorVisible && restarted.Session.InsideBuilding && restarted.Session.Read.Storage().Count==1 && restarted.Session.Read.Storage()[0].Quantity==10 && restarted.Session.Position==savedPosition;
        string detail=$"room={restarted.InteriorVisible},inside={restarted.Session.InsideBuilding},count={restarted.Session.Read.Storage().FirstOrDefault().Quantity},saved={savedPosition},actual={restarted.Session.Position}";
        GetTree().Root.RemoveChild(restarted);Task closed=restarted.shutdown;restarted.QueueFree();await NextFrame(SceneTree.SignalName.ProcessFrame);await closed;
        Farmer.Camera.MakeCurrent();
        check(restoreOk,"actual scene restart restores indoor position and chest: "+detail);
        check(!GodotObject.IsInstanceValid(restarted),"restored interior scene and actor release on teardown");
        Session.Player.Bag["carrot"]=4;Session.Move(home.Bed.X,home.Bed.Y);int day=Session.Read.World.Day;
        InteractBuilding();check(Hud.ModalVisible && Session.Read.World.Day==day,"bed interaction warns about bag before sleeping");Hud.CloseModal();
        check(Session.Read.Inventory()[0].Quantity==4 && Session.Read.World.Day==day,"cancel bed sleep preserves state");
        InteractBuilding();var sleep=Hud.FindChild("ConfirmSleep",true,false) as Button ?? throw new InvalidOperationException("bed confirmation missing");sleep.EmitSignal(Button.SignalName.Pressed);
        check(Session.Read.World.Day==day+1 && Session.Read.Inventory()[0].Quantity==3 && Session.Read.Storage()[0].Quantity==10 && InteriorVisible,"actual bed sleep advances day with original bag spoilage and retained chest");
        Session.Move(home.Chest.X,home.Chest.Y);Hud.OpenStorage();
        var withdraw=Hud.FindChild("StorageWithdraw-carrot",true,false) as Button ?? throw new InvalidOperationException("withdraw button missing");withdraw.EmitSignal(Button.SignalName.Pressed);
        check(Session.Read.Storage()[0].Quantity==9 && Session.Read.Inventory()[0].Quantity==4,"storage UI withdrawal conserves produce");Hud.CloseModal();
        Session.ToggleInteriorLights();check(!Session.Read.InteriorLightsOn,"indoor lighting setting changes and persists in session");
        Session.State.Minute=1200;Session.State.Weather="พายุ";
        for(int n=0;n<60;n++)await NextFrame(SceneTree.SignalName.ProcessFrame);
        if(DisplayServer.GetName()!="headless") {
            await NextDraw();using var shot=GetViewport().GetTexture().GetImage();shot.SavePng(Path.Combine(ProjectSettings.GlobalizePath("res://reports"),"farmhouse-night.png"));
            var frames=new List<double>();
            long previousTick=System.Diagnostics.Stopwatch.GetTimestamp();
            for(int n=0;n<300;n++) {
                await NextFrame(SceneTree.SignalName.ProcessFrame);long tick=System.Diagnostics.Stopwatch.GetTimestamp();frames.Add(System.Diagnostics.Stopwatch.GetElapsedTime(previousTick,tick).TotalMilliseconds);previousTick=tick;
            }
            frames.Sort();double mean=frames.Average(),p95=frames[(int)(frames.Count*.95)];
            indoorMeasurement=new {frames=frames.Count,quality=Quality,meanMs=mean,p95Ms=p95,fps=1000/mean,privateBytes=System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64,managedBytes=GC.GetTotalMemory(false),scope="300 actual rendered night/storm farmhouse frames after settling, not a long-session guarantee"};
            check(mean<=17 && p95<=18,"rendered farmhouse satisfies GDD mean and P95 frame targets");
        }
        check(!weather.Visible && !accents.Visible && !weather.IsProcessing(),"storm visuals remain outside roof at night");
        Session.State.Minute=360;Session.ToggleInteriorLights();Session.Player.Bag.Clear();
        await Session.SaveAsync();
        // Cancel a restoring scene before its first transition completes; callbacks must not touch freed nodes.
        var interrupted=template.Instantiate<FarmScene>();interrupted.BuildingDiagnosticDirectory=copy;GetTree().Root.AddChild(interrupted);
        GetTree().Root.RemoveChild(interrupted);Task interruptedShutdown=interrupted.shutdown;interrupted.QueueFree();await NextFrame(SceneTree.SignalName.ProcessFrame);await interruptedShutdown;
        Farmer.Camera.MakeCurrent();check(!GodotObject.IsInstanceValid(interrupted),"scene freed during interior restore drains cancelled transition safely");
    }
}
