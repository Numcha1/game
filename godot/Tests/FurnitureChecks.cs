using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Nodes;

static class FurnitureChecks
{
    public static Task<List<string>> Run(string reports)
    {
        var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
        string profile=Path.Combine(reports,"furniture-profile-"+Guid.NewGuid().ToString("N"));
        var house=BuildingRegistry.Farmhouse;var workshop=BuildingRegistry.Find("village.workshop")!;
        string file=Path.Combine(profile,"farm.json");Guid buy=Guid.NewGuid(),deposit=Guid.NewGuid(),placed=Guid.NewGuid();string box=placed.ToString("N");byte[] v2;
        void Rowan(GameSession game) {
            if(game.InsideBuilding)game.ExitBuilding(true);game.State.Minute=600;game.Move(23,8);game.EnterBuilding(NpcRegistry.Village);
            game.Move(workshop.Entrance.X,workshop.Entrance.Y);game.EnterBuilding(workshop.Id);game.Move(12,8);
        }
        void House(GameSession game) {if(game.InsideBuilding)game.ExitBuilding(true);game.Move(house.Entrance.X,house.Entrance.Y);game.EnterBuilding(house.Id);}
        var game=new GameSession(new SaveService(profile));
        try {
            game.State.Coins=10000;
            Check(!game.BuyFurniture("furniture:wooden-box",1,buy).Changed && game.Buildings.Furniture==null,"remote purchase leaves v1 state unmigrated");
            Rowan(game);game.SaveAsync().GetAwaiter().GetResult();v2=File.ReadAllBytes(file);
            Check(JsonDocument.Parse(v2).RootElement.GetProperty("Version").GetInt32()==2,"existing village profile starts as exact v2 document");
            int coins=game.State.Coins;
            Check(!game.BuyFurniture("wooden-box",1,Guid.NewGuid()).Changed && !game.BuyFurniture("furniture:wooden-box",0,Guid.NewGuid()).Changed && !game.BuyFurniture("furniture:wooden-box",1,Guid.Empty).Changed,"aliases invalid quantities and absent command IDs cannot buy");
            Check(game.BuyFurniture("furniture:wooden-box",1,buy).Changed && game.State.Coins==coins-50 && game.CarriedCount("furniture:wooden-box")==1,"box purchase charges once and creates carried item");
            Check(!game.BuyFurniture("furniture:wooden-box",1,buy).Changed && game.State.Coins==coins-50,"replayed purchase does not duplicate or charge");
            game.SaveAsync().GetAwaiter().GetResult();
            Check(File.ReadAllBytes(file+".before-furniture-v3").SequenceEqual(v2),"explicit v3 migration permanently preserves original v2 bytes");
            Check(JsonDocument.Parse(File.ReadAllBytes(file)).RootElement.GetProperty("Version").GetInt32()==3 && game.State.Version==1,"new envelope v3 preserves legacy farm version one");
            House(game);
            Check(game.PreviewFurniture("furniture:wooden-box",8,9,0)=="","box valid preview uses real farmhouse collision and paths");
            Check(game.PreviewFurniture("furniture:wooden-box",12,14,0)!="" && game.PreviewFurniture("furniture:wooden-box",4,5,0)!="" && game.PreviewFurniture("furniture:wooden-box",double.NaN,9,0)!="" && game.PreviewFurniture("furniture:wooden-box",8.1,9,0)!="","exit static bed invalid coordinates and off-grid placement rejected");
            Check(game.PlaceFurniture("furniture:wooden-box",8,9,0,placed).Changed && game.CarriedCount("furniture:wooden-box")==0,"placement consumes exactly one carried box");
            Check(!game.PlaceFurniture("furniture:wooden-box",10,9,0,placed).Changed && game.FurnitureViews().Count==1,"placement replay cannot clone instance");
            game.Buildings.Furniture!.AppliedCommands.Remove(placed);
            game.Buildings.Furniture.Carried[game.Player.Id]["furniture:wooden-box"]=1;
            Check(!game.PlaceFurniture("furniture:wooden-box",10,9,0,placed).Changed && game.CarriedCount("furniture:wooden-box")==1,"expired ledger placement UUID cannot collide with live instance or consume carried item");
            game.Buildings.Furniture.Carried[game.Player.Id].Remove("furniture:wooden-box");game.Buildings.Furniture.AppliedCommands.Add(placed);
            Check(!game.Move(8,8.6),"placed object blocks original player movement");
            Check(!game.TransferFurniture(box,"carrot",1,true,Guid.NewGuid()).Changed,"remote furniture transfer rejected");
            game.Move(8,9.8);game.Player.Bag["carrot"]=20;game.Buildings.Village.Seeds["carrot"]=4;
            Check(game.TransferFurniture(box,"carrot",7,true,deposit).Changed && game.Player.Bag["carrot"]==13,"deposit moves original produce rather than copying");
            Check(!game.TransferFurniture(box,"carrot",7,true,deposit).Changed && game.Player.Bag["carrot"]==13,"deposit replay cannot consume twice");
            Check(!game.TransferFurniture(box,"carrot",14,true,Guid.NewGuid()).Changed && !game.TransferFurniture(box,"carrot",8,false,Guid.NewGuid()).Changed && !game.TransferFurniture(box,"tool:water",1,true,Guid.NewGuid()).Changed && !game.TransferFurniture(box,null!,1,true,Guid.NewGuid()).Changed,"invalid transfers preserve source and destination");
            Check(!game.PickUpFurniture(box,Guid.NewGuid()).Changed && game.FurnitureViews().Single().Contents["carrot"]==7,"nonempty pickup protects contents");
            Check(game.TransferFurniture(box,"seed:carrot",2,true,Guid.NewGuid()).Changed && game.SeedCount("carrot")==2 && !game.Player.Bag.ContainsKey("seed:carrot"),"seed storage routes original separate seed inventory without polluting produce");
            Check(game.OrganizeFurniture(box,Guid.NewGuid()).Changed,"container organize runs validated command");
            Check(!game.PlaceFurniture("furniture:wooden-box",8,10.5,0,Guid.NewGuid(),box).Changed,"moving cannot cover player standing position");game.Move(9.4,9.8);
            Check(game.PlaceFurniture("furniture:wooden-box",8,10.5,0,Guid.NewGuid(),box).Changed && game.FurnitureViews().Single().Contents["carrot"]==7,"moving retains instance identity and contents");
            game.Move(8,11.3);game.SaveAsync().GetAwaiter().GetResult();
        } finally {game.DisposeAsync().AsTask().GetAwaiter().GetResult();}
        game=new GameSession(new SaveService(profile));
        try {
            Check(game.ActiveBuildingId==house.Id && game.FurnitureViews().Single().Id==box && game.FurnitureViews().Single().Contents["carrot"]==7,"box contents and placement restore inside real farmhouse");
            Check(!game.TransferFurniture(box,"carrot",7,true,deposit).Changed,"successful furniture command ledger survives restart");
            Check(game.TransferFurniture(box,"carrot",7,false,Guid.NewGuid()).Changed && game.Player.Bag["carrot"]==20,"withdraw restores exact original aggregate");
            Check(game.TransferFurniture(box,"seed:carrot",2,false,Guid.NewGuid()).Changed && game.SeedCount("carrot")==4,"withdraw seed restores original prepaid seed dictionary");
            var pickup=Guid.NewGuid();Check(game.PickUpFurniture(box,pickup).Changed && game.CarriedCount("furniture:wooden-box")==1 && game.FurnitureViews().Count==0,"empty box pickup returns single inventory item");
            Check(!game.PickUpFurniture(box,pickup).Changed && game.CarriedCount("furniture:wooden-box")==1,"pickup replay cannot duplicate carried item");
            // First box loop is complete before exercising the rest of the catalog.
            Rowan(game);foreach(var def in PlaceableFurnitureCatalog.All)Check(game.BuyFurniture(def.ItemId,1,Guid.NewGuid()).Changed,"functional Rowan purchase "+def.Id);
            Check(game.BuyFurniture(PlaceableFurnitureCatalog.WoodId,50,Guid.NewGuid()).Changed,"wood material bought through authoritative merchant");House(game);
            var positions=new[]{new WorldPoint(8,7),new WorldPoint(11,7),new WorldPoint(15,7),new WorldPoint(8,10),new WorldPoint(15,10),new WorldPoint(19,10)};
            for(int i=0;i<6;i++) {
                var d=PlaceableFurnitureCatalog.All[i];var point=positions[i];
                Check(game.PlaceFurniture(d.ItemId,point.X,point.Y,0,Guid.NewGuid()).Changed,"valid six-type placement "+d.Id);
            }
            Check(!game.PlaceFurniture("furniture:wooden-box",8,7,0,Guid.NewGuid()).Changed,"new object cannot overlap placed table");
            var views=game.FurnitureViews();var table=views.Single(p=>p.DefinitionId=="wooden-table");game.Move(table.X,table.Y+.8);
            Check(game.TransferFurniture(table.Id,"carrot",1,true,Guid.NewGuid()).Changed && game.Player.Bag["carrot"]==19,"table display consumes actual inventory item");
            Check(!game.TransferFurniture(table.Id,"carrot",1,true,Guid.NewGuid()).Changed && game.FurnitureViews().Single(p=>p.Id==table.Id).Contents["carrot"]==1,"display limits one item per visible entry");
            Check(game.PlaceFurniture("furniture:wooden-table",8,7,1,Guid.NewGuid(),table.Id).Changed,"table rotates with swapped footprint and retained display");
            var chair=views.Single(p=>p.DefinitionId=="wooden-chair");game.Move(chair.X,chair.Y+.8);
            Check(game.SitFurniture(chair.Id).Changed && !game.Move(12,9) && !game.PickUpFurniture(chair.Id,Guid.NewGuid()).Changed,"seat action blocks movement and pickup while occupied");
            Check(game.SitFurniture(chair.Id).Changed && game.SeatedFurnitureId=="","standing restores controls without changing saved collision position");
            var shelf=views.Single(p=>p.DefinitionId=="bookshelf");game.Move(shelf.X,shelf.Y+.8);
            Check(game.TransferFurniture(shelf.Id,PlaceableFurnitureCatalog.WoodId,1,true,Guid.NewGuid()).Changed,"shelf displays real carried material");
            var work=views.Single(p=>p.DefinitionId=="crafting-workbench");game.Move(work.X,work.Y+.8);int wood=game.CarriedCount(PlaceableFurnitureCatalog.WoodId),boxes=game.CarriedCount("furniture:wooden-box");var craft=Guid.NewGuid();
            Check(game.CraftFurniture(work.Id,"furniture:wooden-box",craft).Changed && game.CarriedCount(PlaceableFurnitureCatalog.WoodId)==wood-8 && game.CarriedCount("furniture:wooden-box")==boxes+1,"workbench crafting atomically consumes wood and produces carried box");
            Check(!game.CraftFurniture(work.Id,"furniture:wooden-box",craft).Changed && !game.CraftFurniture(work.Id,"unknown",Guid.NewGuid()).Changed,"craft replay and unknown recipe do not mutate inventory");
            var storage=views.Single(p=>p.DefinitionId=="storage-chest");game.Move(storage.X,storage.Y+.8);
            Check(game.TransferFurniture(storage.Id,"carrot",1,true,Guid.NewGuid()).Changed,"second storage type shares real inventory transfer");
            int originalCarrots=game.Player.Bag["carrot"];game.Player.Bag["carrot"]=int.MaxValue;
            Check(!game.TransferFurniture(storage.Id,"carrot",1,false,Guid.NewGuid()).Changed && game.FurnitureViews().Single(p=>p.Id==storage.Id).Contents["carrot"]==1,"withdraw overflow cannot lose item from container");game.Player.Bag["carrot"]=originalCarrots;
            Check(game.TransferFurniture(storage.Id,"carrot",1,false,Guid.NewGuid()).Changed,"valid withdraw works after rejected overflow");
            var woodenBox=views.Single(p=>p.DefinitionId=="wooden-box");game.Move(woodenBox.X,woodenBox.Y+.8);
            var fixtureItems=GameCatalog.CropKeys.Concat(GameCatalog.CropKeys.Take(4).Select(ItemCatalog.SeedId)).ToArray();
            foreach(string item in fixtureItems){var itemDef=ItemCatalog.Find(item)!;if(itemDef.Category==ItemCategory.Seed)game.Buildings.Village.Seeds[itemDef.CropId!]=2;else game.Player.Bag[item]=2;Check(game.TransferFurniture(woodenBox.Id,item,1,true,Guid.NewGuid()).Changed,"box slot accepts compatible inventory ID "+item);}
            int heldWood=game.CarriedCount(PlaceableFurnitureCatalog.WoodId);
            Check(!game.TransferFurniture(woodenBox.Id,PlaceableFurnitureCatalog.WoodId,1,true,Guid.NewGuid()).Changed && game.CarriedCount(PlaceableFurnitureCatalog.WoodId)==heldWood,"thirteenth distinct item cannot exceed twelve-slot box or consume source");
            foreach(string item in fixtureItems)Check(game.TransferFurniture(woodenBox.Id,item,1,false,Guid.NewGuid()).Changed,"slot withdrawal restores source "+item);
            Check(game.FurnitureViews().Single(p=>p.Id==woodenBox.Id).Contents.Count==0,"full box withdrawal returns every distinct item");
            game.Move(storage.X,storage.Y+.8);Check(game.TransferFurniture(storage.Id,"furniture:wooden-box",1,true,Guid.NewGuid()).Changed && game.TransferFurniture(storage.Id,"furniture:wooden-box",1,false,Guid.NewGuid()).Changed,"container safely carries stateless furniture inventory item");
            game.Move(chair.X,chair.Y+.8);Check(!game.PlaceFurniture("furniture:wooden-chair",11,7,2,Guid.NewGuid(),chair.Id).Changed,"unsupported third chair orientation rejected");
            game.SaveAsync().GetAwaiter().GetResult();
            var saved=File.ReadAllBytes(file);var json=JsonNode.Parse(saved)!;
            bool Reject(JsonNode node){try{SaveService.Deserialize(System.Text.Encoding.UTF8.GetBytes(node.ToJsonString()));return false;}catch(InvalidDataException){return true;}}
            var broken=json.DeepClone();broken["Version"]=2;Check(Reject(broken),"v3 furniture cannot be smuggled into v2 save");
            broken=json.DeepClone();broken["Buildings"]!["Furniture"]!["Schema"]=99;Check(Reject(broken),"future unknown furniture schema rejected without dropping data");
            broken=json.DeepClone();broken["Buildings"]!["Furniture"]!["Placed"]![table.Id]!["Contents"]!["carrot"]=-1;Check(Reject(broken),"corrupt contents refused");
            broken=json.DeepClone();broken["Buildings"]!["Furniture"]!["Placed"]![table.Id]!["X"]=12;broken["Buildings"]!["Furniture"]!["Placed"]![table.Id]!["Y"]=14;Check(Reject(broken),"corrupt blocked exit placement refused");
            Check(File.ReadAllBytes(file).SequenceEqual(saved),"malformed fixtures never overwrite tested valid save");
        } finally {game.DisposeAsync().AsTask().GetAwaiter().GetResult();}
        game=new GameSession(new SaveService(profile));
        try {Check(game.FurnitureViews().Count==6 && game.FurnitureViews().Single(p=>p.DefinitionId=="wooden-table").Contents["carrot"]==1 && game.FurnitureViews().Single(p=>p.DefinitionId=="bookshelf").Contents[PlaceableFurnitureCatalog.WoodId]==1,"all six types and displayed inventory restore together");}
        finally {game.DisposeAsync().AsTask().GetAwaiter().GetResult();}
        return Task.FromResult(checks);
    }
}
