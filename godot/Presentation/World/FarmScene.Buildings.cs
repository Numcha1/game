using Godot;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;

public partial class FarmScene
{
    readonly InteriorSceneLoader interiorLoader=new();
    readonly List<(CanvasItem Node,bool Visible)> hiddenExterior=new();
    InteriorRoom? interior;
    ColorRect doorFade=null!;
    Label doorPrompt=null!;
    Sprite2D doorPulse=null!;
    public bool DiagnosticAudioMuted=>IsDiagnosticSession;
    bool transitioning;
    Task pendingTransition=Task.CompletedTask;
    internal string? DiagnosticInteriorSceneOverride;
    public bool InteriorVisible=>interior!=null;
    public string DoorPrompt=>doorPrompt.Text;
    public Task TransitionCompletion=>pendingTransition;
    void InitializeBuildings()
    {
        var layer=new CanvasLayer {Layer=8};AddChild(layer);
        doorFade=new ColorRect {AnchorRight=1,AnchorBottom=1,Color=new Color(.08f,.10f,.08f,0),MouseFilter=Control.MouseFilterEnum.Ignore};layer.AddChild(doorFade);
        doorPrompt=new Label {AnchorLeft=.5f,AnchorRight=.5f,AnchorTop=1,AnchorBottom=1,OffsetLeft=-300,OffsetRight=300,OffsetTop=-175,OffsetBottom=-140,
            HorizontalAlignment=HorizontalAlignment.Center,Theme=FarmTypography.Shared,AutowrapMode=TextServer.AutowrapMode.WordSmart};
        doorPrompt.AddThemeColorOverride("font_shadow_color",new Color(.1f,.15f,.1f));doorPrompt.AddThemeConstantOverride("shadow_offset_x",2);doorPrompt.AddThemeConstantOverride("shadow_offset_y",2);layer.AddChild(doorPrompt);
        doorPrompt.AddThemeColorOverride("font_color",new Color("fff2cc"));
        doorPulse=new Sprite2D {Texture=Art.Glow,ZIndex=6,Visible=false,Scale=Vector2.One*.22f};AddChild(doorPulse);
        AddChild(new Sprite2D {Name="VillageRouteSign",Texture=Art.Region("sign"),Position=new Vector2(726,236)});
        var route=new Label {Name="VillageRouteLabel",Text="หมู่บ้าน →",Theme=FarmTypography.Shared,Position=new Vector2(670,206),Size=new Vector2(110,24)};
        route.AddThemeFontSizeOverride("font_size",13);route.AddThemeColorOverride("font_color",new Color("fff0ca"));route.AddThemeColorOverride("font_shadow_color",new Color("24392b"));route.AddThemeConstantOverride("shadow_offset_x",1);route.AddThemeConstantOverride("shadow_offset_y",1);AddChild(route);
        if(Session.InsideBuilding)pendingTransition=TransitionBuilding(Session.ActiveBuildingId,false,true);
    }
    void UpdateDoorPrompt()
    {
        doorPulse.Visible=transitioning && !Session.InsideBuilding && !Display.ReducedMotion;
        if(doorPulse.Visible)doorPulse.Modulate=new Color(1,.8f,.45f,.35f+.15f*Mathf.Sin(Time.GetTicksMsec()/60f));
        if(transitioning || TitleActive || Hud.ModalVisible){doorPrompt.Text="";return;}
        var point=Session.Position;
        string npc=Session.NearestNpc();
        if(npc!=""){Session.TryNpc(npc,out var view);doorPrompt.Text="F · คุยกับ"+view.Name;return;}
        var nearby=BuildingRegistry.All.FirstOrDefault(b=>b.ParentId==Session.ActiveBuildingId && point.Distance(b.Entrance)<=1.3);
        if(nearby!=null){doorPrompt.Text="F · เข้า"+nearby.Name;return;}
        if(BuildingRegistry.Find(Session.ActiveBuildingId) is { } inside) {
            doorPrompt.Text=point.Distance(inside.Exit)<=1.4 ? inside.ParentId==""?"F · กลับฟาร์ม":"F · กลับหมู่บ้าน" : point.Distance(inside.Bed)<=1.6 ? "F / Space · เข้านอน" : point.Distance(inside.Chest)<=1.6 ? "F · เปิดกล่องเก็บของ" : inside.Name+" · F โต้ตอบ · Esc เมนู";
        } else {
            var building=BuildingRegistry.All.FirstOrDefault(b=>b.ParentId=="" && point.Distance(b.Entrance)<=1.8);
            doorPrompt.Text=building==null?"":"F · เข้า"+building.Name;
        }
    }
    public void InteractBuilding()
    {
        if(!ReadyToPlay || IsBusy || Hud.ModalVisible || TitleActive)return;
        string npc=Session.NearestNpc();if(npc!=""){Hud.OpenNpc(npc);return;}
        var nearby=BuildingRegistry.All.FirstOrDefault(b=>b.ParentId==Session.ActiveBuildingId && Session.Position.Distance(b.Entrance)<=1.3);
        if(nearby!=null){RequestBuildingEntry(nearby.Id);return;}
        if(BuildingRegistry.Find(Session.ActiveBuildingId) is { } def) {
            if(Session.Position.Distance(def.Exit)<=1.4)RequestBuildingExit();
            else if(Session.Position.Distance(def.Bed)<=1.6)Hud.RequestSleep();
            else if(Session.Position.Distance(def.Chest)<=1.6)Hud.OpenStorage();
            else Notify("เดินใกล้จุดโต้ตอบก่อน");
        } else {
            var building=BuildingRegistry.All.FirstOrDefault(b=>b.ParentId=="" && Session.Position.Distance(b.Entrance)<=1.8);
            if(building!=null)RequestBuildingEntry(building.Id);
        }
    }
    public void RequestBuildingEntry(string id)
    {
        if(!ReadyToPlay || IsBusy || Hud.ModalVisible || TitleActive)return;
        if(BuildingRegistry.Find(id) is not { } building){Notify("ไม่พบอาคารนี้");return;}
        string error=Session.EntryError(building);if(error!=""){Notify(error);return;}
        doorPulse.Position=new Vector2((float)building.Entrance.X*32,(float)building.Entrance.Y*32-15);
        pendingTransition=TransitionBuilding(id,false,false);
    }
    public void RequestBuildingExit(bool emergency=false)
    {
        if(!ReadyToPlay || IsBusy || !Session.InsideBuilding)return;
        pendingTransition=TransitionBuilding(Session.ActiveBuildingId,true,emergency);
    }
    async Task FadeDoor(float target)
    {
        if(Display.ReducedMotion){doorFade.Color=new Color(.08f,.10f,.08f,target);return;}
        float from=doorFade.Color.A;double elapsed=0;
        while(elapsed<.18) {
            await NextFrame(SceneTree.SignalName.ProcessFrame);elapsed+=GetProcessDeltaTime();
            doorFade.Color=new Color(.08f,.10f,.08f,Mathf.Lerp(from,target,(float)Math.Clamp(elapsed/.18,0,1)));
        }
    }
    async Task TransitionBuilding(string id,bool exit,bool restoreOrEmergency)
    {
        transitioning=true;Farmer.InputBlocked=true;InspectPlot(-1);
        InteriorRoom? candidate=null;bool committed=false;
        try {
            var def=BuildingRegistry.Find(id) ?? throw new InvalidDataException("อาคารไม่รองรับ");
            var destination=exit && !restoreOrEmergency && def.ParentId!=""?BuildingRegistry.Find(def.ParentId):exit?null:def;
            if(destination!=null) {
                var scene=await interiorLoader.Load(IsDiagnosticSession && DiagnosticInteriorSceneOverride!=null ? DiagnosticInteriorSceneOverride : destination.Scene,()=>NextFrame(SceneTree.SignalName.ProcessFrame),sceneLifetime.Token);
                candidate=scene.Instantiate<InteriorRoom>();AddChild(candidate);candidate.Visible=false;candidate.Initialize(Session.Read,destination,this);
            }
            audio.Play("door");await FadeDoor(1);
            var result=exit?Session.ExitBuilding(restoreOrEmergency):restoreOrEmergency?new Core.Application.CommandResult("กลับเข้าบ้านที่บันทึกไว้",true):Session.EnterBuilding(id);
            if(!result.Changed) {Notify(result.Message);await FadeDoor(0);return;}
            committed=true;
            RestoreExterior();
            if(Session.InsideBuilding)ShowInterior(candidate!);
            candidate=null;Notify(result.Message);
            await FadeDoor(0);
        }
        catch(OperationCanceledException) { }
        catch(Exception e)when(e is InvalidDataException or IOException or InvalidCastException) {
            log.Write("error","building.transition.failed",e.GetType().Name);
            if(sceneActive) {
                if(committed || Session.InsideBuilding != (interior!=null)){if(Session.InsideBuilding)Session.ExitBuilding(true);RestoreExterior();}
                doorFade.Color=new Color(.08f,.10f,.08f,0);Notify("เข้าอาคารไม่ได้ กลับฟาร์มได้ตามปกติ: "+e.Message);
            }
        }
        finally {
            if(candidate!=null && GodotObject.IsInstanceValid(candidate))candidate.QueueFree();
            transitioning=false;
            if(sceneActive)Farmer.InputBlocked=operationInProgress || Hud.ModalVisible || TitleActive;
        }
    }
    void ShowInterior(InteriorRoom room)
    {
        interior=room;
        doorPulse.Visible=false;
        Farmer.Reparent(room.GetNode<Node2D>("Actors"));
        foreach(var node in GetChildren())if(node is CanvasItem visual && visual!=room && node.Name!="Ambient") {
            hiddenExterior.Add((visual,visual.Visible));visual.Visible=false;
        }
        weather.SetProcess(false);accents.SetProcess(false);GetNode<CanvasModulate>("Ambient").Color=Colors.White;
        room.Visible=true;ResetBuildingCamera(true);
        accents.PondRipplesEnabled=Session.ActiveBuildingId!=NpcRegistry.Village;
        if(Session.ActiveBuildingId==NpcRegistry.Village){weather.Visible=true;accents.Visible=true;weather.SetProcess(true);accents.SetProcess(true);}
    }
    void RestoreExterior()
    {
        if(interior!=null){Farmer.Reparent(GetNode<Node2D>("Actors"));interior.QueueFree();interior=null;}
        foreach(var entry in hiddenExterior)if(GodotObject.IsInstanceValid(entry.Node))entry.Node.Visible=entry.Visible;
        hiddenExterior.Clear();accents.PondRipplesEnabled=true;weather.SetProcess(true);accents.SetProcess(true);ResetBuildingCamera(false);
    }
    void ResetBuildingCamera(bool indoor)
    {
        var p=Session.Read.Player;Farmer.ResetCamera(new Vector2((float)p.X*32,(float)p.Y*32),indoor?768:800,indoor?512:576);
    }
}
