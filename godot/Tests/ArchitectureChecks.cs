using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Infrastructure.Configuration;
using SeasonsTogether.Core.Infrastructure.Logging;
using System.Text.Json;
static class ArchitectureChecks
{
    public static async Task<List<string>> Run(string reports)
    {
        var checks=new List<string>(); void Check(bool ok,string label) { if (!ok) throw new Exception(label); checks.Add(label); }
        string source=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        string[] forbidden = { "using Godot", "System.Windows.Forms", "System.Drawing" };
        string domain=string.Join('\n',Directory.GetFiles(Path.Combine(source,"Core/Domain"),"*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));
        domain += File.ReadAllText(Path.Combine(source,"../native/World.cs"));
        Check(!forbidden.Any(domain.Contains),"domain and original rules have no engine/UI/graphics dependency");
        string app=string.Join('\n',Directory.GetFiles(Path.Combine(source,"Core/Application"),"*.cs").Select(File.ReadAllText));
        Check(!forbidden.Any(app.Contains) && !app.Contains("using SeasonsTogether.Core.Infrastructure") && !app.Contains("File.Write") && !app.Contains("File.Read"),"application depends on contracts and domain only");
        string presentation=string.Join('\n',Directory.GetFiles(Path.Combine(source,"Presentation"),"*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));
        Check(!System.Text.RegularExpressions.Regex.IsMatch(presentation,@"Session\.(State|Player)\b") && !presentation.Contains("using SuanPleng"),"presentation cannot access mutable legacy state");
        Check(!presentation.Contains("GD.Load") && !presentation.Contains("File.Write") && !presentation.Contains("Farm.Action("),"presentation has no direct disk writes, resource loading or duplicated commands");
        string profile=Path.Combine(reports,"diagnostics-profile-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(profile);
        var settings=new GraphicsSettings(profile); await settings.SaveAsync(0); var middle=settings.SaveAsync(1); var last=settings.SaveAsync(2); await Task.WhenAll(middle,last);
        Check(settings.Load()==2 && !Directory.GetFiles(profile,"*.tmp").Any(),"rapid quality preferences persist last selection without temp files");
        File.WriteAllText(Path.Combine(profile,"graphics.json"),"corrupt"); Check(settings.Load()==2,"corrupt graphics config uses safe default");
        string logPath=Path.Combine(profile,"events.jsonl"); File.WriteAllText(logPath,new string(' ',2*1024*1024+1));
        await using(var log=new JsonGameLog(profile)) log.Write("error","save.failed",nameof(IOException));
        using var doc=JsonDocument.Parse(File.ReadAllText(logPath));
        Check(File.Exists(logPath+".1") && doc.RootElement.GetProperty("Code").GetString()=="save.failed" && doc.RootElement.GetProperty("ExceptionType").GetString()=="IOException", "structured logger rotates and records error code without payload");
        Check(MusicLink.TryParse("https://www.youtube.com/watch?v=abc",out _) && MusicLink.TryParse("https://youtu.be/abc",out _),"existing YouTube link variants preserved");
        Check(!MusicLink.TryParse("https://youtube.com.evil.test/",out _) && !MusicLink.TryParse("https://user:secret@youtube.com/",out _) && !MusicLink.TryParse("http://youtube.com/",out _) && !MusicLink.TryParse("https://youtube.com:8443/",out _),"music shell boundary rejects deceptive hosts, credentials, HTTP and custom ports");
        return checks;
    }
}
