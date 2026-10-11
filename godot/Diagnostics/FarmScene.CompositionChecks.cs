using Godot;
using SeasonsTogether.Core.Domain;
using System.Text.Json;
namespace SeasonsTogether;
public partial class FarmScene
{
    async Task RunCompositionChecks()
    {
        bool before=arguments.Contains("--composition-before");
        string stage=arguments.Contains("--composition-seed")?"seed":arguments.Contains("--composition-exterior")?"exterior":"all";
        var checks=new List<string>();var frames=new List<object>();
        void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
        string reports=ProjectSettings.GlobalizePath("res://reports/composition");Directory.CreateDirectory(reports);
        async Task Leave(){Session.State.Paused=false;while(Session.InsideBuilding){Session.Move(12,14);RequestBuildingExit();await TransitionCompletion;}}
        async Task Enter(string id){await Leave();Session.State.Minute=600;Session.State.Weather="แดดอ่อน";if(id!="moonroot.farmhouse"){Session.Move(23,8);RequestBuildingEntry(NpcRegistry.Village);await TransitionCompletion;}if(id!=NpcRegistry.Village){var d=BuildingRegistry.Find(id)!;Session.Move(d.Entrance.X,d.Entrance.Y);RequestBuildingEntry(id);await TransitionCompletion;}Check(Session.ActiveBuildingId==id,"actual entry "+id);}
        var ids=stage=="seed"?new[]{"village.seed-shop"}:stage=="exterior"?new[]{"farm",NpcRegistry.Village}:new[]{"village.seed-shop","moonroot.farmhouse","village.workshop","village.mira-home","village.rowan-home","farm",NpcRegistry.Village};
        foreach(string id in ids) {
            if(id=="farm")await Leave();else await Enter(id);
            foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1600,900),new Vector2I(1920,1080)}) {
                Session.State.Paused=false;Session.State.Minute=600;Session.State.Weather="แดดอ่อน";Session.State.Day=1;
                GetWindow().Size=size;Session.Move(12,id=="farm"?8:id==NpcRegistry.Village?10:9.5);
                Session.State.Paused=true;Hud.ClearFeedback();Hud.Refresh();
                Farmer.ResetCamera(new Vector2((float)Session.Position.X*32,(float)Session.Position.Y*32),Session.InsideBuilding?768:800,Session.InsideBuilding?512:576,Session.InsideBuilding && id!=NpcRegistry.Village);
                for(int i=0;i<100;i++)await NextFrame(SceneTree.SignalName.PhysicsFrame);
                Check(GetWindow().Size==size,"actual resolution "+id+" "+size);
                var top=Hud.GetChildren().OfType<PanelContainer>().First();var buttons=Hud.GetChildren().OfType<HBoxContainer>().First().GetChildren().OfType<Button>().ToArray();
                Check(!top.GetGlobalRect().Intersects(buttons[0].GetGlobalRect()),"HUD separation "+id+" "+size);
                Check(GetViewport().GetVisibleRect().HasPoint(GetViewport().GetCanvasTransform()*Farmer.GlobalPosition),"player visible "+id+" "+size);
                var stable=GetViewport().GetCanvasTransform();for(int i=0;i<10;i++)await NextFrame(SceneTree.SignalName.PhysicsFrame);
                Check(GetViewport().GetCanvasTransform().IsEqualApprox(stable),"stable camera "+id+" "+size);
                if(!before){Check(Farmer.Sprite.Scale==Vector2.One && Farmer.Camera.Zoom==Vector2.One,"uniform pixel scale "+id+" "+size);if(interior!=null){using var pixels=interior.GetNode<Sprite2D>("Background").Texture.GetImage();Check(pixels.GetPixel(400,300).A>.99f,"opaque prepared room floor "+id+" "+size);}}
                await NextDraw();using var texture=GetViewport().GetTexture();using var shot=texture.GetImage();
                string name=$"{(before?"before":"after")}-{id}-{size.X}x{size.Y}.png";shot.SavePng(Path.Combine(reports,name));
                frames.Add(new{scene=id,window=size.ToString(),position=Session.Position,time=Session.State.Minute,weather=Session.State.Weather,season=0,cameraPosition=Farmer.Camera.GlobalPosition.ToString(),zoom=Farmer.Camera.Zoom.X,spriteScale=Farmer.Sprite.Scale.X,shotWidth=shot.GetWidth(),shotHeight=shot.GetHeight(),screenshot=name});
            }
        }
        await Leave();await Session.SaveAsync();finalCheckpoint=true;
        File.WriteAllText(Path.Combine(reports,$"{(before?"before":"after")}-{stage}.json"),JsonSerializer.Serialize(new{passed=checks.Count,checks,frames},new JsonSerializerOptions{WriteIndented=true}));
        GD.Print("COMPOSITION PASS "+checks.Count);GetTree().CallDeferred(SceneTree.MethodName.Quit,0);
    }
}
