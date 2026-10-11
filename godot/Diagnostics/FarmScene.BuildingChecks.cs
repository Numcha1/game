using Godot;
using SeasonsTogether.Core.Domain;
using System.Text.Json;

namespace SeasonsTogether;

public partial class FarmScene
{
    static bool buildingHarnessRunning;
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
                Check(!Session.Move(4.5,5) && !Session.Move(-1,8) && !Session.Execute(new("plant",0,"carrot")).Changed,"bed wall collision and outdoor action isolation "+round);
                if(round==0 && DisplayServer.GetName()!="headless") {
                    await NextDraw();using var shot=GetViewport().GetTexture().GetImage();shot.SavePng(Path.Combine(ProjectSettings.GlobalizePath("res://reports"),"farmhouse-interior.png"));
                }
                Session.Move(12,14);RequestBuildingExit();await TransitionCompletion;
                Check(!Session.InsideBuilding && !InteriorVisible && Session.Position.Distance(BuildingRegistry.Farmhouse.Return)<.05 && weather.Visible && !Farmer.InputBlocked,"correct safe exterior return "+round);
            }
            Check(interiorLoader.CachedCount==1,"interior scene asset cached across repeated entry");
            await Session.SaveAsync();finalCheckpoint=true;
            string output=Path.Combine(ProjectSettings.GlobalizePath("res://reports"),"buildings-"+DisplayServer.GetName()+".json");
            await System.IO.File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=checks.Count,checks,scope="Real Godot farmhouse entrance/exit/resource failure/actor/collision tests in isolated profile"},new JsonSerializerOptions{WriteIndented=true}));
            GD.Print("BUILDINGS PASS "+checks.Count);GetTree().Quit();
        } catch(Exception e){if(sceneActive){GD.PushError("BUILDINGS FAIL "+e);GetTree().Quit(1);}}
    }
}
