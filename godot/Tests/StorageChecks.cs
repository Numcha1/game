using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Infrastructure.Persistence;
using System.Text;
using System.Text.Json.Nodes;

static class StorageChecks
{
    public static async Task<List<string>> Run(string reports)
    {
        var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
        string profile=Path.Combine(reports,"storage-profile-"+Guid.NewGuid().ToString("N"));
        var home=BuildingRegistry.Farmhouse;string path=Path.Combine(profile,"farm.json");
        await using(var game=new GameSession(new SaveService(profile))) {
            Check(!game.TransferStorage("carrot",1,true,Guid.NewGuid()).Changed,"outside chest transactions refused");
            game.Move(home.Return.X,home.Return.Y);game.EnterBuilding(home.Id);
            game.Player.Bag["carrot"]=10;game.Move(12,10);
            Check(!game.TransferStorage("carrot",1,true,Guid.NewGuid()).Changed,"remote chest transaction refused");
            game.Move(home.Chest.X,home.Chest.Y);
            Check(!game.TransferStorage("unknown",1,true,Guid.NewGuid()).Changed && !game.TransferStorage("carrot",0,true,Guid.NewGuid()).Changed && !game.TransferStorage("carrot",-1,true,Guid.NewGuid()).Changed && !game.TransferStorage("carrot",1,true,Guid.Empty).Changed,"invalid item quantity and missing UUID refused");
            Check(!game.TransferStorage("carrot",11,true,Guid.NewGuid()).Changed && game.Player.Bag["carrot"]==10,"insufficient source leaves bag unchanged");
            Guid id=Guid.NewGuid();Check(game.TransferStorage("carrot",6,true,id).Changed,"deposit validated produce");
            Check(!game.TransferStorage("carrot",6,true,id).Changed && game.Read.Storage()[0].Quantity==6 && game.Player.Bag["carrot"]==4,"repeated storage UUID cannot duplicate items");
            var detached=game.Read.Storage();game.TransferStorage("carrot",2,false,Guid.NewGuid());
            Check(detached[0].Quantity==6 && game.Read.Storage()[0].Quantity==4 && game.Player.Bag["carrot"]==6,"withdraw conserves items and storage view detaches");
            game.Player.Bag["carrot"]=int.MaxValue;
            Check(!game.TransferStorage("carrot",1,false,Guid.NewGuid()).Changed,"withdraw rejects integer overflow before mutation");
            game.Player.Bag["carrot"]=InteriorState.MaximumStorageStack+1;
            Check(!game.TransferStorage("carrot",InteriorState.MaximumStorageStack,true,Guid.NewGuid()).Changed,"deposit validates maximum stored stack before mutation");game.Player.Bag["carrot"]=6;
            game.Execute(new("pause"));Check(!game.TransferStorage("carrot",1,true,Guid.NewGuid()).Changed,"paused chest transaction refused");game.Execute(new("pause"));
            Check(!game.Execute(new("sleep")).Changed,"indoor sleep requires actual bed proximity");
            game.Move(home.Bed.X,home.Bed.Y);int day=game.Read.World.Day;
            Check(game.Execute(new("sleep")).Changed && game.Read.World.Day==day+1 && game.Read.Player.Energy==100 && game.InsideBuilding,"bed delegates original new-day rules and preserves room");
            Check(game.Player.Bag["carrot"]==4 && game.Read.Storage()[0].Quantity==4,"legacy bag spoilage preserved and stored produce retained");
            Check(game.ToggleInteriorLights().Changed && !game.Read.InteriorLightsOn,"indoor light choice updates authoritative state");
            game.Tick(15);game.FlushAsync().GetAwaiter().GetResult();
        }
        await using(var reloaded=new GameSession(new SaveService(profile))) {
            Check(reloaded.InsideBuilding && !reloaded.Read.InteriorLightsOn && reloaded.Read.Storage()[0].Quantity==4 && reloaded.Read.Inventory()[0].Quantity==4,"restart preserves room lights chest and bag from one autosave snapshot");
            reloaded.ExitBuilding(true);reloaded.EnterBuilding(home.Id);
            Check(reloaded.Read.Storage()[0].Quantity==4 && !reloaded.Read.InteriorLightsOn,"re-entry retains existing chest and light state");
            reloaded.Move(home.Chest.X,home.Chest.Y);
            reloaded.TransferStorage("carrot",4,false,Guid.NewGuid());reloaded.SaveAsync().GetAwaiter().GetResult();
            Check(reloaded.Read.Storage().Count==0 && reloaded.Read.Inventory()[0].Quantity==8,"full withdrawal removes empty stack without losing produce");
            reloaded.TransferStorage("carrot",3,true,Guid.NewGuid());reloaded.SaveAsync().GetAwaiter().GetResult();
        }
        byte[] valid=File.ReadAllBytes(path);
        foreach(string fault in new[]{"missing-buildings","missing-locations","missing-farm-day","unknown-storage-item","negative-stack","unknown-field","invalid-level"}) {
            var json=JsonNode.Parse(valid)!;
            switch(fault) {
                case "missing-buildings":json.AsObject().Remove("Buildings");break;
                case "missing-locations":json["Buildings"]!.AsObject().Remove("Locations");break;
                case "missing-farm-day":json["Farm"]!.AsObject().Remove("Day");break;
                case "unknown-storage-item":json["Buildings"]!["Interiors"]![home.Id]!["Storage"]!["unknown"]=1;break;
                case "negative-stack":json["Buildings"]!["Interiors"]![home.Id]!["Storage"]!["carrot"]=-1;break;
                case "unknown-field":json["Buildings"]!["Interiors"]![home.Id]!["FutureFurniture"]=1;break;
                case "invalid-level":json["Buildings"]!["Interiors"]![home.Id]!["Level"]=3;break;
            }
            bool refused=false;try{SaveService.Deserialize(Encoding.UTF8.GetBytes(json.ToJsonString()));}catch(Exception e)when(e is InvalidDataException or System.Text.Json.JsonException){refused=true;}
            Check(refused && File.ReadAllBytes(path).SequenceEqual(valid),"version-two strict contract refuses "+fault);
        }
        // The backup must restore farm inventory and chest together, never combine revisions.
        var backup=JsonNode.Parse(File.ReadAllBytes(path+".bak"))!;
        File.WriteAllText(path,"{\"Version\":2}");
        await SaveService.RestoreBackupAsync(profile);
        var recovered=JsonNode.Parse(File.ReadAllBytes(path))!;
        Check(JsonNode.DeepEquals(backup,recovered) && Directory.GetFiles(profile,"farm.json.corrupt-*").Length==1,"explicit backup recovery restores full extended revision and retains damaged bytes");
        bool malformedVersion=false;try{SaveService.Deserialize(Encoding.UTF8.GetBytes("{\"Version\":\"2\"}"));}catch(Exception e)when(e is InvalidDataException or System.Text.Json.JsonException){malformedVersion=true;}
        Check(malformedVersion,"nonnumeric version stays within handled save error boundary");
        string collision=Path.Combine(reports,"storage-profile-"+Guid.NewGuid().ToString("N"));
        await using(var game=new GameSession(new SaveService(collision))) {
            game.Move(home.Return.X,home.Return.Y);game.SaveAsync().GetAwaiter().GetResult();
            string live=Path.Combine(collision,"farm.json");byte[] corrupt=Encoding.UTF8.GetBytes("{broken");File.WriteAllBytes(live,corrupt);
            game.EnterBuilding(home.Id);
            bool notified=false;try{game.FlushAsync().WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();}catch(IOException){notified=true;}
            Check(notified && File.ReadAllBytes(live).SequenceEqual(corrupt),"migration reader failure completes flush with error and preserves live bytes");
        }
        return checks;
    }
}
