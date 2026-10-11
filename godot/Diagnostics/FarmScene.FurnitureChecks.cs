using Godot;
using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Infrastructure.Persistence;
using SeasonsTogether.Core.Application;
using System.Text.Json;

namespace SeasonsTogether;
public partial class FarmScene
{
    async Task RunFurnitureChecks()
    {
        var checks=new List<string>();void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);checks.Add(label);}
        string reports=ProjectSettings.GlobalizePath("res://reports");
        string suffix=arguments.Contains("--furniture-box-only")?"box-loop":"six-types";
        async Task Shot(string name){if(DisplayServer.GetName()=="headless")return;await NextDraw();using var texture=GetViewport().GetTexture();using var image=texture.GetImage();image.SavePng(Path.Combine(reports,name+".png"));}
        Button Find(string name)=>(Button)(Hud.FindChild(name,true,false)??throw new InvalidOperationException("Missing UI button "+name));
        void Click(string name)=>Find(name).EmitSignal(Button.SignalName.Pressed);
        void F(){using var ev=new InputEventKey{PhysicalKeycode=Key.F,Pressed=true};_UnhandledInput(ev);}
        async Task Frames(int n=3){for(int i=0;i<n;i++)await NextFrame(SceneTree.SignalName.ProcessFrame);}
        async Task Measure(string name) {
            Hud.OpenHelp();Hud.CloseModal(); // Identical hidden UI nodes in before/after measurements.
            await Frames(20);var samples=new List<double>();var cpu=new List<double>();long startMemory=(long)Performance.GetMonitor(Performance.Monitor.MemoryStatic);int startNodes=(int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);ulong started=Time.GetTicksUsec(),last=started;
            for(int i=0;i<600;i++){await NextFrame(SceneTree.SignalName.ProcessFrame);ulong now=Time.GetTicksUsec();samples.Add((now-last)/1000d);last=now;cpu.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000);}
            double fps=600000000d/(last-started);samples.Sort();cpu.Sort();Check((int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)==startNodes,name+" stable node count across 600 frames");
            System.IO.File.WriteAllText(Path.Combine(reports,"furniture-performance-"+name+".json"),JsonSerializer.Serialize(new{frames=600,fps,medianFrameMs=samples[300],p95FrameMs=samples[570],processP95Ms=cpu[570],memoryStart=startMemory,memoryEnd=(long)Performance.GetMonitor(Performance.Monitor.MemoryStatic),managedMemory=GC.GetTotalMemory(false),nodes=startNodes,resources=(int)Performance.GetMonitor(Performance.Monitor.ObjectResourceCount),objects=furnitureRoom!.PlacedVisualCount,renderer=DisplayServer.GetName()},new JsonSerializerOptions{WriteIndented=true}));
        }
        async Task Enter(string id){RequestBuildingEntry(id);await TransitionCompletion;await Frames();Check(Session.ActiveBuildingId==id && InteriorVisible,"actual scene transition "+id);}
        async Task Exit(){Session.Move(12,14);RequestBuildingExit();await TransitionCompletion;await Frames();}
        void PointInput(WorldPoint point,bool confirm)
        {
            Vector2 view=GetViewport().GetCanvasTransform()*new Vector2((float)point.X*32,(float)point.Y*32);
            using var motion=new InputEventMouseMotion{Position=view};_UnhandledInput(motion);
            if(confirm){using var click=new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=view};_UnhandledInput(click);}
        }
        async Task PlaceFromBag(string definition,WorldPoint point)
        {
            Hud.OpenBag();Click("BagFilter-All");Click("Item-furniture_"+definition);
            Check(FurniturePlacementActive && !Hud.ModalVisible,"real inventory starts preview "+definition);
            PointInput(point,false);await Frames();
            Check(furnitureRoom!.GetNode<Sprite2D>("FurniturePreview").Visible,"ghost drawn inside farmhouse "+definition);
            PointInput(point,true);await Frames();Check(!FurniturePlacementActive && Session.FurnitureViews().Any(p=>p.DefinitionId==definition),"real mouse placement commits "+definition);
        }
        try {
            await Frames();Session.State.Minute=600;Session.State.Coins=10000;Session.Move(23,8);await Enter(NpcRegistry.Village);
            var workshop=BuildingRegistry.Find("village.workshop")!;Session.Move(workshop.Entrance.X,workshop.Entrance.Y);await Enter(workshop.Id);Session.Move(12,8);F();
            Check(Hud.ModalVisible && Hud.FindChild("OpenFurnitureShop",true,false)!=null,"F opens Rowan real workshop furniture shop");Click("OpenFurnitureShop");
            int coins=Session.Read.World.Coins;Click("BuyFurniture-furniture_wooden-box");
            Check(Session.CarriedCount("furniture:wooden-box")==1 && Session.Read.World.Coins==coins-50,"real shop UI purchases box into backpack");
            Hud.CloseModal();await Exit();await Exit();Session.Move(3.5,4);await Enter(BuildingRegistry.Farmhouse.Id);
            if(!arguments.Contains("--furniture-box-only")){SetQuality(2);await Measure("empty-farmhouse");}
            await PlaceFromBag("wooden-box",new(8,9));var box=Session.FurnitureViews().Single().Id;
            Check(furnitureRoom!.PlacedVisualCount==1 && !Session.Move(8,8.6),"box has actual scene sprite and collision");
            Session.Player.Bag["carrot"]=10;Session.Move(8,9.8);await Frames();F();
            Check(Hud.ModalVisible && Farmer.InputBlocked && Hud.FindChild("FurnitureDeposit-carrot",true,false)!=null,"F opens real storage interface and blocks world input");
            ((SpinBox)Hud.FindChild("FurnitureTransferQuantity",true,false)).Value=4;Click("FurnitureDeposit-carrot");
            Check(Session.Read.Inventory().Single(i=>i.Crop=="carrot").Quantity==6 && Session.FurnitureViews().Single().Contents["carrot"]==4,"real deposit UI transfers original four produce");
            Click("FurniturePickup");Check(Hud.ModalVisible && Session.FurnitureViews().Count==1,"nonempty pickup UI protects stored items");
            Click("FurnitureOrganize");await Shot("furniture-box-storage");
            await Session.SaveAsync();var decoded=BuildingSaveDocument.Decode(await System.IO.File.ReadAllBytesAsync(Session.SavePath));
            Check(decoded.Buildings.Furniture!.Placed[box].Contents["carrot"]==4 && decoded.Farm.Version==1,"actual atomic v3 save contains box and original farm");
            ((SpinBox)Hud.FindChild("FurnitureTransferQuantity",true,false)).Value=4;Click("FurnitureWithdraw-carrot");Click("FurniturePickup");await Frames();
            Check(!Hud.ModalVisible && Session.CarriedCount("furniture:wooden-box")==1 && Session.FurnitureViews().Count==0 && Session.Read.Inventory().Single(i=>i.Crop=="carrot").Quantity==10,"real withdraw and pickup completes first furniture loop without loss");
            if(!arguments.Contains("--furniture-box-only")) {
                await Exit();Session.Move(23,8);await Enter(NpcRegistry.Village);Session.Move(workshop.Entrance.X,workshop.Entrance.Y);await Enter(workshop.Id);Session.Move(12,8);F();Click("OpenFurnitureShop");
                foreach(var def in PlaceableFurnitureCatalog.All.Where(d=>d.Id!="wooden-box")){Click("BuyFurniture-"+def.ItemId.Replace(':','_'));Check(Session.CarriedCount(def.ItemId)==1,"shop UI supports "+def.Id);}
                ((SpinBox)Hud.FindChild("FurnitureBuyQuantity",true,false)).Value=50;Click("BuyFurniture-material_wood");Check(Session.CarriedCount(PlaceableFurnitureCatalog.WoodId)==50,"real UI buys crafting material");
                Hud.CloseModal();await Exit();await Exit();Session.Move(3.5,4);await Enter(BuildingRegistry.Farmhouse.Id);
                var points=new[]{new WorldPoint(8,7),new WorldPoint(11,7),new WorldPoint(15,7),new WorldPoint(8,10),new WorldPoint(15,10),new WorldPoint(19,10)};
                for(int i=0;i<6;i++)await PlaceFromBag(PlaceableFurnitureCatalog.All[i].Id,points[i]);
                Check(furnitureRoom!.PlacedVisualCount==6,"six actual furniture actors sorted with farmhouse player");
                var table=Session.FurnitureViews().Single(p=>p.DefinitionId=="wooden-table");Session.Move(table.X,table.Y+.8);await Frames();F();Click("FurnitureDeposit-carrot");Hud.CloseModal();await Frames();
                Check(furnitureRoom.GetNode<Node2D>("Placed-"+table.Id).GetNodeOrNull<Sprite2D>("Displayed-carrot") is {Visible:true},"table shows consumed inventory item as actual sprite");
                Check(Session.Read.Inventory().Single(i=>i.Crop=="carrot").Quantity==9,"display places a real item from original produce bag");
                F();Click("FurnitureWithdraw-carrot");Hud.CloseModal();await Frames();Check(Session.Read.Inventory().Single(i=>i.Crop=="carrot").Quantity==10 && furnitureRoom.GetNode<Node2D>("Placed-"+table.Id).GetNodeOrNull<Sprite2D>("Displayed-carrot")==null,"real display retrieval restores inventory and removes item sprite");F();Click("FurnitureDeposit-carrot");Hud.CloseModal();
                F();Click("FurnitureMove");PointInput(new(8,7),false);using(var rotate=new InputEventKey{PhysicalKeycode=Key.R,Pressed=true})_UnhandledInput(rotate);
                PointInput(new(8,7),true);await Frames();Check(Session.FurnitureViews().Single(p=>p.Id==table.Id).Rotation==1 && Session.FurnitureViews().Single(p=>p.Id==table.Id).Contents["carrot"]==1,"R rotation retains display and selects authored vertical table artwork");
                var chair=Session.FurnitureViews().Single(p=>p.DefinitionId=="wooden-chair");Session.Move(chair.X,chair.Y+.8);await Frames();F();Click("FurnitureSit");await Frames();
                Check(Session.SeatedFurnitureId==chair.Id && !Hud.ModalVisible && !Session.Move(12,9) && Farmer.Sprite.Scale.Y<Farmer.Sprite.Scale.X,"chair sit action changes actual player pose and blocks movement");F();await Frames();
                Check(Session.SeatedFurnitureId=="" && Session.Move(12,9),"F stands and restores real player movement");
                var chest=Session.FurnitureViews().Single(p=>p.DefinitionId=="storage-chest");Session.Move(chest.X,chest.Y+.8);await Frames();F();Click("FurnitureDeposit-carrot");Check(Session.FurnitureViews().Single(p=>p.Id==chest.Id).Contents["carrot"]==1,"actual storage chest UI deposits item");Click("FurnitureWithdraw-carrot");Hud.CloseModal();Check(Session.FurnitureViews().Single(p=>p.Id==chest.Id).Contents.Count==0,"actual storage chest UI withdraw conserves item");
                var shelf=Session.FurnitureViews().Single(p=>p.DefinitionId=="bookshelf");Session.Move(shelf.X,shelf.Y+.8);await Frames();F();Click("FurnitureDeposit-material_wood");Hud.CloseModal();await Frames();
                Check(furnitureRoom.GetNode<Node2D>("Placed-"+shelf.Id).GetNodeOrNull<Sprite2D>("Displayed-material_wood")!=null,"shelf visibly displays actual carried material");
                var bench=Session.FurnitureViews().Single(p=>p.DefinitionId=="crafting-workbench");Session.Move(bench.X,bench.Y+.8);await Frames();F();int wood=Session.CarriedCount(PlaceableFurnitureCatalog.WoodId);Click("FurnitureCraft-wooden-box");
                Check(Session.CarriedCount(PlaceableFurnitureCatalog.WoodId)==wood-8 && Session.CarriedCount("furniture:wooden-box")==1,"workbench recipe UI consumes real wood and creates carried box");await Shot("furniture-workbench");Hud.CloseModal();
                BeginFurniturePlacement("furniture:wooden-box");PointInput(new(12,14),true);Check(FurniturePlacementActive && Session.CarriedCount("furniture:wooden-box")==1,"real preview rejects blocked exit without consuming item");
                using(var cancel=new InputEventKey{PhysicalKeycode=Key.Escape,Pressed=true})_UnhandledInput(cancel);Check(!FurniturePlacementActive,"Esc cancels real placement without consumption");
                Session.Move(12,11.5);await Frames();await Shot("furniture-farmhouse-six-types");await Session.SaveAsync();
                string restore=Path.Combine(reports,"furniture-profile-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(restore);System.IO.File.Copy(Session.SavePath,Path.Combine(restore,"farm.json"));System.IO.File.Copy(Path.Combine(DataDirectory,"player.txt"),Path.Combine(restore,"player.txt"));
                var template=ResourceLoader.Load<PackedScene>("res://Scenes/Farm.tscn");var restarted=template.Instantiate<FarmScene>();restarted.BuildingDiagnosticDirectory=restore;GetTree().Root.AddChild(restarted);
                await restarted.TransitionCompletion;await Frames();
                Check(restarted.InteriorVisible && restarted.furnitureRoom!.PlacedVisualCount==6 && restarted.Session.FurnitureViews().Single(p=>p.Id==table.Id).Contents["carrot"]==1 && restarted.Session.CarriedCount("furniture:wooden-box")==1,"actual Godot scene restart restores all six objects displays and crafting inventory");
                GetTree().Root.RemoveChild(restarted);Task closed=restarted.shutdown;restarted.QueueFree();await Frames();await closed;Farmer.Camera.MakeCurrent();
                Check(!GodotObject.IsInstanceValid(restarted),"restored furniture scene releases owned nodes on teardown");await Frames(15);
                foreach(int quality in new[]{0,1,2}){SaveQuality(quality);await Measure("six-types-"+new[]{"low","medium","high"}[quality]);}
            }
            await Session.SaveAsync();finalCheckpoint=true;
            System.IO.File.WriteAllText(Path.Combine(reports,"furniture-"+suffix+"-"+DisplayServer.GetName()+".json"),JsonSerializer.Serialize(new{passed=checks.Count,checks},new JsonSerializerOptions{WriteIndented=true}));
            GD.Print("FURNITURE PASS "+suffix+" "+checks.Count);GetTree().CallDeferred(SceneTree.MethodName.Quit,0);
        } catch(Exception error){GD.PushError(error.ToString());GetTree().Quit(1);}
    }
}
