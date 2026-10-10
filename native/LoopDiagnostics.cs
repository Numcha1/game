using System.Diagnostics;
using System.Text.Json;

namespace SuanPleng;
public sealed partial class GameForm {
    internal async Task<RoomClient> PrepareLoopTest() {
        var farm=new Farm{Weather="พายุ",Minute=1180};string[] crops=Farm.Crops.Keys.ToArray();
        for(int i=0;i<24;i++){string crop=crops[i%8];farm.Plots[i]=new(){Crop=crop,Growth=Farm.Crops[crop].Days,RipeDay=1};}
        server=new RoomServer(farm);server.ClaimOwner(session);client=new RoomClient(server.Url,server.Token);
        Apply(await client.Send(new Request{Session=session,Name="เจ้าของ",Action="join"}));
        var peer=new RoomClient(server.Url,server.Token);await peer.Send(new Request{Session=LoopDiagnostics.PeerSession,Name="เพื่อน",Action="join"});
        connected=true;network.Start();return peer;
    }
    internal void SetLoopMotion(bool right){keys.Clear();keys.Add(right?Keys.D:Keys.A);}
    internal int PeerCount=>world?.Players.Count??0;
    internal void LoopRender(Graphics graphics)=>OnPaint(new PaintEventArgs(graphics,new Rectangle(Point.Empty,ClientSize)));
}
static class LoopDiagnostics {
    internal static readonly string PeerSession=Guid.NewGuid().ToString("N");
    public static void Run(string output) {
        Directory.CreateDirectory(output);File.Delete(Path.Combine(output,"loop-benchmark.json"));
        using var form=new GameForm(Path.Combine(output,"profile"));form.CreateControl();_=form.Handle;
        using var bitmap=new Bitmap(1280,800);using var graphics=Graphics.FromImage(bitmap);
        using var context=new ApplicationContext();using var bootstrap=new System.Windows.Forms.Timer{Interval=1};
        using var peerTimer=new System.Windows.Forms.Timer{Interval=250};
        RoomClient? peer=null;bool peerBusy=false;var intervals=new List<double>();var renders=new List<double>();
        var clock=new Stopwatch();double previous=0;int polls=0;
        peerTimer.Tick+=async(_,_)=>{if(peerBusy||peer==null)return;peerBusy=true;try{await peer.Send(new Request{Session=PeerSession,Action="sync"});polls++;}catch(Exception e){File.WriteAllText(Path.Combine(output,"loop-error.txt"),e.Message);}finally{peerBusy=false;}};
        bootstrap.Tick+=async(_,_)=>{bootstrap.Stop();try{peer=await form.PrepareLoopTest();peerTimer.Start();clock.Start();}catch(Exception e){File.WriteAllText(Path.Combine(output,"loop-error.txt"),e.ToString());context.ExitThread();}};
        form.FrameStep+=()=>{
            if(!clock.IsRunning)return;double now=clock.Elapsed.TotalSeconds;form.SetLoopMotion((int)now%2==0);
            long start=Stopwatch.GetTimestamp();form.LoopRender(graphics);double render=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if(now>1&&previous>0){intervals.Add((now-previous)*1000);renders.Add(render);}previous=now;
            if(now<11)return;
            peerTimer.Stop();bootstrap.Stop();clock.Stop();
            double seconds=intervals.Sum()/1000;var sorted=intervals.Order().ToArray();
            File.WriteAllText(Path.Combine(output,"loop-benchmark.json"),JsonSerializer.Serialize(new{method="Actual WinForms frame and network timers, two local protocol clients, moving character, full crop farm and storm/night. Rendered into an offscreen bitmap; excludes display presentation.",frames=intervals.Count,seconds,fps=intervals.Count/seconds,meanIntervalMs=intervals.Average(),p95IntervalMs=sorted[(int)(sorted.Length*.95)],meanRenderMs=renders.Average(),peerPolls=polls,players=form.PeerCount},new JsonSerializerOptions{WriteIndented=true}));
            context.ExitThread();
        };
        bootstrap.Start();Application.Run(context);peer?.Dispose();
        if(!File.Exists(Path.Combine(output,"loop-benchmark.json")))throw new InvalidOperationException("Loop test did not finish");
    }
}
