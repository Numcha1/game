using Godot;
using SeasonsTogether.Core.Domain;
using System.Text.Json;
namespace SeasonsTogether;

public partial class FarmScene
{
    async Task RunIndoorLayoutChecks()
    {
        var checks=new List<string>();
        var measurements=new List<object>();
        void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
        bool before=arguments.Contains("--indoor-before");
        string reports=ProjectSettings.GlobalizePath("res://reports");
        Session.State.Minute=600;
        var shop=BuildingRegistry.Find("village.seed-shop")!;
        Session.Move(shop.Entrance.X,shop.Entrance.Y);RequestBuildingEntry(shop.Id);await TransitionCompletion;
        Check(Session.ActiveBuildingId==shop.Id,"actual seed shop entry");
        for(int i=0;i<320;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
        foreach(var size in new[]{new Vector2I(1280,720),new Vector2I(1920,1080)}) {
            GetWindow().Size=size;
            Session.Move(12,9.5);
            for(int i=0;i<70;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(GetWindow().Size==size,"actual window resolution "+size);
            var hudPanels=Hud.GetChildren().OfType<PanelContainer>().ToArray();
            var top=hudPanels[0];
            var buttons=Hud.GetChildren().OfType<HBoxContainer>().First().GetChildren().OfType<Button>().ToArray();
            measurements.Add(new{resolution=size.ToString(),viewport=GetViewport().GetVisibleRect().Size.ToString(),cameraZoom=Farmer.Camera.Zoom.X,playerScale=Farmer.Sprite.Scale.X,topHeight=top.Size.Y,buttonHeights=buttons.Select(b=>b.Size.Y).ToArray(),prompt=DoorPrompt});
            Check(buttons.Length==3,"three top controls "+size);
            Check(!top.GetGlobalRect().Intersects(buttons[0].GetGlobalRect()),"top panels do not overlap "+size);
            if(!before) {
                Check(Mathf.IsEqualApprox(Farmer.Camera.Zoom.X,1) && Mathf.IsEqualApprox(Farmer.Sprite.Scale.X,1),"indoor zoom and visual-only scale "+size);
                Check(buttons.All(b=>Mathf.IsEqualApprox(b.Size.Y,buttons[0].Size.Y)),"consistent top button height "+size);
                Check(DoorPrompt.Contains("มิรา"),"single contextual Thai NPC prompt "+size);
                var canvas=GetViewport().GetCanvasTransform();
                var left=canvas*new Vector2(-16,0);var right=canvas*new Vector2(784,512);var view=GetViewport().GetVisibleRect();
                Check(left.X<=1 && left.Y<=1 && right.X>=view.Size.X-1 && right.Y>=view.Size.Y-1,"camera covers viewport without room edge gaps "+size);
                Check(Farmer.GlobalPosition==Farmer.GlobalPosition.Round(),"pixel snapped actor "+size);
                Check(Mathf.IsEqualApprox(Farmer.Sprite.Position.Y/Farmer.Sprite.Scale.Y,-22),"original player foot anchor retained "+size);
                var resident=interior!.GetNode<Node2D>("Actors").GetChildren().OfType<NpcActor>().Single(a=>a.Name=="Npc-mira");
                var npcSprite=resident.GetChildren().OfType<AnimatedSprite2D>().Single();
                Check(Mathf.IsEqualApprox(npcSprite.Scale.X,1) && Farmer.Scale==Vector2.One,"NPC visual scale and unchanged actor transform "+size);
                var settled=GetViewport().GetCanvasTransform();
                for(int i=0;i<10;i++)await NextFrame(SceneTree.SignalName.PhysicsFrame);
                Check(GetViewport().GetCanvasTransform().IsEqualApprox(settled),"standing camera is stable "+size);
                Notify("ทดสอบผลตอบกลับการกระทำ");
                for(int i=0;i<3;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
                Check(Hud.FeedbackVisible && DoorPrompt=="","fresh feedback replaces prompt at same target "+size);
                Session.Move(12,14);for(int i=0;i<3;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
                Check(!Hud.FeedbackVisible && DoorPrompt.Contains("F"),"new interaction target replaces stale feedback "+size);
                Session.Move(12,9.5);for(int i=0;i<70;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
            }
            await NextDraw();using(var texture=GetViewport().GetTexture())using(var image=texture.GetImage())image.SavePng(Path.Combine(reports,$"indoor-{(before?"before":"after")}-{size.X}x{size.Y}.png"));
            if(!before) {
                Hud.OpenNpc("mira");Check(Hud.ModalVisible && Farmer.InputBlocked,"real Mira interaction "+size);
                await NextFrame(SceneTree.SignalName.ProcessFrame);Check(DoorPrompt=="","prompt hidden behind modal "+size);Hud.CloseModal();
                Session.Move(12,11);for(int i=0;i<20;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
                Check(DoorPrompt=="","no prompt without target "+size);
                Check(!Session.Move(-2,8) && !Session.Move(12,2),"original room collision retained "+size);
                Session.Move(12,11);
                var start=Session.Position;
                using(var key=new InputEventKey{PhysicalKeycode=Key.D,Pressed=true})Input.ParseInputEvent(key);
                for(int i=0;i<12;i++)await NextFrame(SceneTree.SignalName.PhysicsFrame);
                using(var key=new InputEventKey{PhysicalKeycode=Key.D,Pressed=false})Input.ParseInputEvent(key);
                Check(Session.Position.X>start.X && Farmer.Sprite.Animation=="east","real indoor walking input and animation "+size);
                foreach(var p in new[]{new WorldPoint(1.5,10),new WorldPoint(22,10),new WorldPoint(12,14)}) {
                    Session.Move(p.X,p.Y);for(int i=0;i<70;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
                    var screen=GetViewport().GetCanvasTransform()*Farmer.GlobalPosition;
                    Check(GetViewport().GetVisibleRect().HasPoint(screen),"player visible across room "+p+" "+size);
                }
                Hud.SetTextScale(2);Hud.SetUiScale(2);
                for(int i=0;i<3;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);
                Check(!top.GetGlobalRect().Intersects(buttons[0].GetGlobalRect()) && buttons.All(b=>Hud.GetGlobalRect().Encloses(b.GetGlobalRect())),"large Thai text and UI stay inside safe layout "+size);
                Hud.SetTextScale(0);Hud.SetUiScale(0);
            }
        }
        Session.Move(12,14);RequestBuildingExit();await TransitionCompletion;
        Session.Move(12,14);RequestBuildingExit();await TransitionCompletion;
        Check(!Session.InsideBuilding && Mathf.IsEqualApprox(Farmer.Camera.Zoom.X,1) && Mathf.IsEqualApprox(Farmer.Sprite.Scale.X,1),"outdoor camera and character restored");
        await Session.SaveAsync();finalCheckpoint=true;
        File.WriteAllText(Path.Combine(reports,"indoor-layout-"+(before?"before":"after")+".json"),JsonSerializer.Serialize(new{passed=checks.Count,checks,measurements},new JsonSerializerOptions{WriteIndented=true}));
        GD.Print("INDOOR PASS "+checks.Count);GetTree().CallDeferred(SceneTree.MethodName.Quit,0);
    }
}
