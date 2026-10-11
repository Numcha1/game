using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SeasonsTogether.Core.Infrastructure.Persistence;
using SuanPleng;

internal static class SaveContractChecks
{
    public static async Task<List<string>> Run(string root)
    {
        var checks=new List<string>();
        void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
        bool Refused(byte[] bytes){try{SaveService.Deserialize(bytes);return false;}catch(Exception e)when(e is JsonException or InvalidDataException){return true;}}
        var farm=new Farm { Coins=987 };string secret=Guid.NewGuid().ToString("N");var player=farm.Join(secret,"คู่สัญญาเซฟ");farm.Owner=player.Id;player.Bag["carrot"]=2;
        string original=JsonSerializer.Serialize(farm);
        Check(SaveService.Deserialize(Encoding.UTF8.GetBytes(original)).Coins==987,"complete original v1 including derived fields accepted by strict contract");
        var legacy=JsonNode.Parse(original)!.AsObject();legacy.Remove("Effects");legacy.Remove("EffectSequence");
        Check(SaveService.Deserialize(Encoding.UTF8.GetBytes(legacy.ToJsonString())).Effects.Count==0,"pre-animation v1 optional effects remain readable");
        foreach(string required in new[]{"Version","Day","Minute","Coins","Plots","Players"}) {
            var incomplete=JsonNode.Parse(original)!.AsObject();incomplete.Remove(required);
            Check(Refused(Encoding.UTF8.GetBytes(incomplete.ToJsonString())),"missing critical save member refused: "+required);
        }
        var unsupported=new List<(string Name,JsonObject Data)>();
        var future=JsonNode.Parse(original)!.AsObject();future["Progression"]=new JsonObject { ["Level"]=7 };unsupported.Add(("root progression",future));
        var soil=JsonNode.Parse(original)!.AsObject();soil["Plots"]![0]!["SoilType"]="clay";unsupported.Add(("plot soil metadata",soil));
        var equipment=JsonNode.Parse(original)!.AsObject();equipment["Players"]![0]!["Equipment"]=new JsonArray("steel-can");unsupported.Add(("player equipment",equipment));
        foreach(var item in unsupported)Check(Refused(Encoding.UTF8.GetBytes(item.Data.ToJsonString())),"unsupported member refused without data loss: "+item.Name);
        Check(Refused(Encoding.UTF8.GetBytes("{\"Coins\":1,"+original[1..])),"duplicate root transaction value refused");
        Check(Refused(Encoding.UTF8.GetBytes("{\"Co\\u0069ns\":1,"+original[1..])),"escaped duplicate root key refused");
        string duplicateBag=original.Replace("\"carrot\":2","\"carrot\":2,\"carrot\":9",StringComparison.Ordinal);
        Check(Refused(Encoding.UTF8.GetBytes(duplicateBag)),"duplicate item stack refused instead of silently replacing quantity");
        Check(Refused(Encoding.UTF8.GetBytes("{}")),"empty object cannot silently become a fresh farm");
        foreach(var encoding in new Encoding[]{new UTF8Encoding(true),new UnicodeEncoding(false,true),new UnicodeEncoding(true,true),new UTF32Encoding(false,true),new UTF32Encoding(true,true)}) {
            byte[] bytes=encoding.GetPreamble().Concat(encoding.GetBytes(original)).ToArray();
            Check(SaveService.Deserialize(bytes).Find(secret)?.Bag["carrot"]==2,"strict contract retains BOM compatibility "+encoding.WebName);
        }
        string directory=Path.Combine(root,"strict-save-profile-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        string path=Path.Combine(directory,"farm.json");byte[] futureBytes=Encoding.UTF8.GetBytes(future.ToJsonString());File.WriteAllBytes(path,futureBytes);File.WriteAllText(Path.Combine(directory,"player.txt"),secret);
        await using(var storage=new SaveService(directory)) {
            bool rejected=false;try{storage.Load();}catch(JsonException){rejected=true;}
            Check(rejected,"live profile with unsupported state refuses startup");
        }
        Check(futureBytes.SequenceEqual(File.ReadAllBytes(path)) && !File.Exists(path+".bak"),"refused startup and disposal leave original unsupported save bytes untouched");
        string source=Path.Combine(directory,"source"),target=Path.Combine(directory,"target");Directory.CreateDirectory(source);
        File.WriteAllBytes(Path.Combine(source,"farm.json"),futureBytes);File.WriteAllText(Path.Combine(source,"player.txt"),secret);
        await using(var storage=new SaveService(target)) {
            storage.RequestSave(farm);await storage.FlushAsync();byte[] previous=File.ReadAllBytes(storage.SavePath);
            bool rejected=false;try{await storage.ImportLegacyAsync(source);}catch(JsonException){rejected=true;}
            Check(rejected && previous.SequenceEqual(File.ReadAllBytes(storage.SavePath)) && !File.Exists(storage.SavePath+".import-pending"),"unsupported import rejected before journal or destination replacement");
        }
        Check(futureBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(source,"farm.json"))),"unsupported import source remains byte-identical");
        foreach(string fault in new[]{"unknown","duplicate","missing-farm","missing-identity"}) {
            string profile=Path.Combine(directory,"journal-"+fault);Directory.CreateDirectory(profile);
            string livePath=Path.Combine(profile,"farm.json"),identityPath=Path.Combine(profile,"player.txt"),journalPath=livePath+".import-pending";
            var live=JsonNode.Parse(original)!.AsObject();live["Coins"]=1111;byte[] liveBytes=Encoding.UTF8.GetBytes(live.ToJsonString());
            File.WriteAllBytes(livePath,liveBytes);File.WriteAllText(identityPath,secret);
            var undo=JsonNode.Parse(JsonSerializer.Serialize(new { Version=1,Farm=Encoding.UTF8.GetBytes(original),Identity=Encoding.UTF8.GetBytes(secret) }))!.AsObject();
            if(fault=="unknown")undo["FutureUndoState"]=new JsonObject { ["Coins"]=2222 };
            if(fault=="missing-farm")undo.Remove("Farm");if(fault=="missing-identity")undo.Remove("Identity");
            string text=undo.ToJsonString();if(fault=="duplicate")text="{\"Version\":1,"+text[1..];byte[] journalBytes=Encoding.UTF8.GetBytes(text);File.WriteAllBytes(journalPath,journalBytes);
            bool refused=false;try{await using var storage=new SaveService(profile);}catch(Exception e)when(e is JsonException or InvalidDataException){refused=true;}
            Check(refused && liveBytes.SequenceEqual(File.ReadAllBytes(livePath)) && File.ReadAllText(identityPath)==secret && journalBytes.SequenceEqual(File.ReadAllBytes(journalPath)),"ambiguous undo journal fails before either profile file changes: "+fault);
        }
        return checks;
    }
}
