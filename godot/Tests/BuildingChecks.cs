using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;
using SeasonsTogether.Core.Infrastructure.Persistence;
using System.Text.Json;

static class BuildingChecks
{
    public static async Task<List<string>> Run(string reports)
    {
        var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
        string profile=Path.Combine(reports,"building-profile-"+Guid.NewGuid().ToString("N"));
        string farmFile=Path.Combine(profile,"farm.json");byte[] original;
        var def=BuildingRegistry.Farmhouse;
        await using(var session=new GameSession(new SaveService(profile))) {
            Check(!session.EnterBuilding("unknown").Changed,"unknown building is rejected");
            Check(!session.EnterBuilding(def.Id).Changed,"building entry validates original exterior distance");
            session.Move(def.Return.X,def.Return.Y);session.SaveAsync().GetAwaiter().GetResult();original=File.ReadAllBytes(farmFile);
            Check(JsonDocument.Parse(original).RootElement.GetProperty("Version").GetInt32()==1,"untouched legacy saves remain version one");
            Check(session.EntryError(def with {Opens=900})!="" && session.EntryError(def with {Spawn=new(-1,2)})!="","closed door and invalid spawn explain rejection");
            Check(session.EnterBuilding(def.Id).Changed && session.InsideBuilding && session.Position==def.Spawn,"entry uses reusable definition spawn");
            Check(!session.EnterBuilding(def.Id).Changed,"duplicate entry does not change state");
            Check(!session.Move(4.5,5) && !session.Move(7,3) && !session.Move(double.NaN,8) && !session.Move(25,8),"bed back-wall collision and invalid coordinates rejected");
            Check(!session.Execute(new("water",0)).Changed,"farming commands rejected inside without spending energy");
            session.Move(12,10);Check(!session.ExitBuilding().Changed,"remote indoor exit rejected");
            session.SaveAsync().GetAwaiter().GetResult();
        }
        Check(File.ReadAllBytes(farmFile+".v1-before-buildings").SequenceEqual(original),"migration retains exact original version-one bytes");
        Check(SaveService.Deserialize(File.ReadAllBytes(farmFile)).Version==1,"version-two envelope retains original version-one farm");
        await using(var restored=new GameSession(new SaveService(profile))) {
            Check(restored.InsideBuilding && restored.Position==new WorldPoint(12,10),"restart restores interior identity and position");
            Check(restored.ExitBuilding(true).Changed && restored.Position==def.Return,"emergency return never strands player");
            restored.SaveAsync().GetAwaiter().GetResult();
        }
        byte[] stable=File.ReadAllBytes(farmFile);
        var node=System.Text.Json.Nodes.JsonNode.Parse(stable)!;node["Buildings"]!["Locations"]!["unknown"] = new System.Text.Json.Nodes.JsonObject{["BuildingId"]=def.Id,["X"]=4,["Y"]=5};
        bool refused=false;try{SaveService.Deserialize(System.Text.Encoding.UTF8.GetBytes(node.ToJsonString()));}catch(InvalidDataException){refused=true;}
        Check(refused && File.ReadAllBytes(farmFile).SequenceEqual(stable),"invalid extended location refused without writing live save");
        return checks;
    }
}

