using System.Text.Json;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Infrastructure.Network;
using SuanPleng;
static class NetworkChecks
{
    sealed class LosingAck : IRoomTransport
    {
        readonly Farm farm = new(); readonly HashSet<string> commands = new();
        public readonly List<RoomRequest> Requests = new(); bool lost;
        public Task<RoomSnapshot> SendAsync(RoomRequest request, CancellationToken ct)
        {
            Requests.Add(request); var p = farm.Join(request.Session,request.Name);
            if (request.Action == "water" && commands.Add(request.Command)) { farm.Action(p,"water",0,"carrot"); if (!lost) { lost = true; throw new IOException("ack lost"); } }
            var copy = JsonSerializer.Deserialize<Farm>(JsonSerializer.Serialize(farm))!;
            return Task.FromResult(new RoomSnapshot(new GameReadModel(copy,copy.Find(request.Session)!),""));
        }
        public void Dispose() { }
    }
    sealed class Stalled : IRoomTransport
    {
        public TaskCompletionSource<RoomSnapshot> Reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Sends;
        public Task<RoomSnapshot> SendAsync(RoomRequest req, CancellationToken ct) { Sends++; Started.TrySetResult(); return Reply.Task; }
        public void Dispose() { }
    }
    public static async Task<List<string>> Run()
    {
        var checks = new List<string>(); void Check(bool ok,string name) { if (!ok) throw new Exception(name); checks.Add(name); }
        string session = Guid.NewGuid().ToString("N");
        using (var server = new RoomServer(new Farm()))
        {
            await using var net = new NetworkService(new HttpRoomTransport(server.Url,server.Token),session,"integration");
            var joined = await net.ConnectAsync(); Check(net.State == ConnectionState.Connected && joined.State.Player.Energy == 100,"new HTTP transport interoperates with unchanged server");
            Guid id = Guid.NewGuid(); var one = await net.CommandAsync(new("water",0,Id:id),6,6);
            var two = await net.CommandAsync(new("water",0,Id:id),6,6);
            Check(one.State.Player.Energy == 95 && two.State.Player.Energy == 95 && two.State.Plot(0).Moisture == 95,"new network service preserves UUID idempotency and authoritative moisture");
            var rejoined = await net.ReconnectAsync(); Check(rejoined.State.Player.Energy == 95,"reconnection retains server player identity");
            await using var bad = new NetworkService(new HttpRoomTransport(server.Url,new string('0',48)),Guid.NewGuid().ToString("N"),"bad");
            bool rejected=false; try { await bad.ConnectAsync(); } catch (RoomRejectedException e) { rejected=e.Status==403; }
            Check(rejected && bad.State == ConnectionState.Disconnected,"authentication rejection is terminal and does not retry");
        }
        var lossy = new LosingAck(); await using (var net = new NetworkService(lossy,session,"lossy"))
        {
            await net.ConnectAsync(); var reply = await net.CommandAsync(new("water",0),6,6);
            var sent = lossy.Requests.Where(r=>r.Action == "water").ToArray();
            Check(sent.Length == 2 && sent[0].Command == sent[1].Command && reply.State.Player.Energy == 95,"lost acknowledgement reconnects and retries exact command UUID once");
            Check(lossy.Requests.Count(r=>r.Action=="join") == 2,"retry establishes same session before resending command");
        }
        var stall = new Stalled(); await using (var net = new NetworkService(stall,session,"timeout",TimeSpan.FromMilliseconds(40)))
        {
            bool timed=false; try { await net.ConnectAsync(); } catch (TimeoutException) { timed=true; }
            Check(timed && stall.Sends == 2 && net.State == ConnectionState.Disconnected && net.Current == null,"timeout is bounded even when adapter ignores cancellation");
        }
        var late = new Stalled(); var closing = new NetworkService(late,session,"late"); var pending=closing.ConnectAsync(); await late.Started.Task;
        await closing.DisposeAsync(); bool cancelled=false; try { await pending; } catch (OperationCanceledException) { cancelled=true; }
        var f=new Farm(); var p=f.Join(session,"late"); late.Reply.SetResult(new(new GameReadModel(f,p),""));
        Check(cancelled && closing.Current == null && closing.State == ConnectionState.Closed,"late snapshot cannot revive disposed network session");
        var cancelledAdapter = new Stalled(); await using (var net = new NetworkService(cancelledAdapter,session,"cancel"))
        {
            using var cts = new CancellationTokenSource(); var task=net.ConnectAsync(cts.Token); await cancelledAdapter.Started.Task; cts.Cancel();
            try { await task; } catch (OperationCanceledException) { }
            Check(cancelledAdapter.Sends==1 && net.Current==null,"user cancellation is not retried");
        }
        bool refused=false; try { using var unsafeAdapter = new HttpRoomTransport("http://example.com",new string('A',48)); } catch (ArgumentException) { refused=true; }
        Check(refused,"remote HTTP bearer endpoint refused");
        return checks;
    }
}
