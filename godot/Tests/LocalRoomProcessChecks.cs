using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Infrastructure.Network;
using SuanPleng;

internal static class LocalRoomProcessChecks
{
    record Setup(string Address,string Token,string Session);
    record Result(int Energy,int Carrots,bool DuplicateStable);
    static async Task<Result> Harvest(NetworkService network)
    {
        var command=new GameCommand("harvest",0,Id:Guid.NewGuid());
        var first=await network.CommandAsync(command,6,6);
        var retry=await network.CommandAsync(command,6,6);
        int Count(RoomSnapshot snapshot)=>snapshot.State.Inventory().Where(i=>i.Crop=="carrot").Sum(i=>i.Quantity);
        return new(retry.State.Player.Energy,Count(retry),first.State.Player.Energy==retry.State.Player.Energy && Count(first)==Count(retry));
    }
    public static async Task Peer()
    {
        try {
            var setup=JsonSerializer.Deserialize<Setup>(await Console.In.ReadLineAsync() ?? "")!;
            await using var network=new NetworkService(new HttpRoomTransport(setup.Address,setup.Token),setup.Session,"local peer");
            await network.ConnectAsync();Console.WriteLine("READY");
            if(await Console.In.ReadLineAsync()!="GO")throw new InvalidDataException("Missing test barrier");
            Console.WriteLine(JsonSerializer.Serialize(await Harvest(network)));
        } catch(Exception e) { Console.Error.WriteLine("Local peer failed: "+e.GetType().Name);Environment.ExitCode=1; }
    }
    public static async Task<List<string>> Run()
    {
        var checks=new List<string>();void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(text);}
        foreach(int players in new[]{2,4}) {
            var farm=new Farm();farm.Plots[0].Crop="carrot";farm.Plots[0].Growth=3;farm.Plots[0].RipeDay=1;
            using var server=new RoomServer(farm);
            await using var host=new NetworkService(new HttpRoomTransport(server.Url,server.Token),Guid.NewGuid().ToString("N"),"local host");
            await host.ConnectAsync();var peers=new List<Process>();
            try {
                for(int i=1;i<players;i++) {
                    var start=new ProcessStartInfo("dotnet") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true };
                    start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);start.ArgumentList.Add("--network-peer");
                    var peer=Process.Start(start) ?? throw new IOException("Peer could not start");peers.Add(peer);
                    // Ephemeral test bearer/session stay in stdin, not command arguments, files or logs.
                    await peer.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new Setup(server.Url,server.Token,Guid.NewGuid().ToString("N"))));
                }
                var ready=await Task.WhenAll(peers.Select(p=>p.StandardOutput.ReadLineAsync())).WaitAsync(TimeSpan.FromSeconds(15));
                Check(ready.All(r=>r=="READY"),$"{players} isolated OS processes join unchanged room protocol");
                foreach(var peer in peers)await peer.StandardInput.WriteLineAsync("GO");
                var own=Harvest(host);
                var lines=await Task.WhenAll(peers.Select(p=>p.StandardOutput.ReadLineAsync())).WaitAsync(TimeSpan.FromSeconds(15));
                var results=lines.Select(line=>JsonSerializer.Deserialize<Result>(line ?? "")!).Append(await own).ToArray();
                foreach(var peer in peers)await peer.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                Check(peers.All(p=>p.ExitCode==0),$"{players} process peers exit successfully");
                var snapshot=await host.SyncAsync(6,6);
                Check(results.Sum(r=>r.Carrots)==3 && snapshot.State.Plot(0).Crop=="",$"{players} concurrent harvesters generate only one three-item harvest");
                Check(results.Sum(r=>r.Energy)==players*100-5,$"{players} concurrent harvesters pay energy once");
                Check(results.All(r=>r.DuplicateStable),$"{players} independent UUID retries cannot duplicate harvest items");
                Check(snapshot.State.World.Coins==160,$"{players} concurrent harvest leaves shared balance unchanged");
            } finally {
                foreach(var peer in peers) { try{if(!peer.HasExited)peer.Kill(entireProcessTree:true);}finally{peer.Dispose();} }
            }
        }
        return checks;
    }
}
