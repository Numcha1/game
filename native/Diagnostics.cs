using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SuanPleng;
public sealed partial class GameForm {
    internal void PaintBenchmark(Graphics graphics, double time) {
        // Keep all fixture players active throughout long quality/resolution sweeps.
        if(world!=null){long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();foreach(var p in world.Players)p.LastSeen=now;}
        previewTime=time;renderPreview=true;Animate(.016);
        OnPaint(new PaintEventArgs(graphics,new Rectangle(Point.Empty,ClientSize)));
    }
    internal int VisualChecks(string output) {
        int checks=0;
        void Assert(bool condition,string label){if(!condition)throw new InvalidOperationException("Visual check: "+label);checks++;}
        renderPreview=true;previewTime=10;uiPanel="";
        var sizes=new[]{new Size(960,640),new Size(1280,800),new Size(1920,1080)};
        foreach(var size in sizes)foreach(float z in new[]{1f,1.35f,1.7f}) {
            ClientSize=size;zoom=z;cameraReady=false;using var bitmap=new Bitmap(size.Width,size.Height);using var graphics=Graphics.FromImage(bitmap);PaintBenchmark(graphics,10);
            for(int i=0;i<24;i++){var tile=TilePoint(i);var middle=new PointF(tile.X+17.5f,tile.Y+17.5f);var screen=WorldToScene(middle);var recovered=SceneToWorld(screen);Assert(Math.Abs(recovered.X-middle.X)<.001&&Math.Abs(recovered.Y-middle.Y)<.001,"camera inverse tile "+i);}
            // Real mouse dispatch goes through letterboxing and camera transforms.
            var centre=WorldToScene(new(TilePoint(0).X+17.5f,TilePoint(0).Y+17.5f));
            if(SceneView.Contains(centre)){connected=true;tool="water";pending.Clear();OnClick(null,new MouseEventArgs(MouseButtons.Left,1,(int)Math.Round(ox+centre.X*scale),(int)Math.Round(oy+centre.Y*scale),0));Assert(pending.Count==1&&pending.Peek().Index==0&&pending.Peek().Action=="water","physical click selects original plot");pending.Clear();connected=false;}
        }
        ClientSize=new(1280,800);zoom=1.35f;cameraReady=false;tool="plant";
        foreach(string panel in new[]{"seeds","bag","market","settings","help","pause"}) {
            uiPanel=panel;using var bitmap=new Bitmap(1280,800);using var graphics=Graphics.FromImage(bitmap);PaintBenchmark(graphics,10);bitmap.Save(Path.Combine(output,"ui-"+panel+".png"));
            Assert(buttons.Count>0&&buttons.All(b=>b.Area.Top>=116&&b.Area.Bottom<=654),"modal owns all buttons "+panel);
            string before=tool;OnClick(null,new MouseEventArgs(MouseButtons.Left,1,355,746,0));Assert(tool==before&&pending.Count==0,"covered hotbar cannot fire "+panel);
        }
        uiPanel="";
        var farm=world!;int savedDay=farm.Day;
        for(int season=0;season<4;season++)foreach(string weather in new[]{"แดดอ่อน","พายุ"}){
            farm.Day=1+season*14;farm.Weather=weather;farm.Minute=weather=="พายุ"?1210:735;
            using var bitmap=new Bitmap(1280,800);using var graphics=Graphics.FromImage(bitmap);PaintBenchmark(graphics,10);bitmap.Save(Path.Combine(output,$"season-{season}-{(weather=="พายุ"?"night":"day")}.png"));checks++;
        }
        farm.Day=savedDay;farm.Minute=735;farm.Weather="แดดอ่อน";
        for(int q=0;q<3;q++){quality=q;reducedMotion=q==0;using var bitmap=new Bitmap(1280,800);using var graphics=Graphics.FromImage(bitmap);PaintBenchmark(graphics,10);checks++;}
        reducedMotion=false;quality=2;AnimationSmoke(output);checks+=5;
        Assert(sparks.Count==0&&sparkPool.Count<=384,"pool bounded after burst");
        previewTime=20;CapturePreviousDay();farm.Day++;farm.Minute=360;
        using(var transition=new Bitmap(1280,800)){using var g=Graphics.FromImage(transition);PaintBenchmark(g,20.25);transition.Save(Path.Combine(output,"day-transition.png"));}
        Assert(previousDayLayer!=null&&dayTransitionStart==20,"day transition caches previous scene");
        farm.Day=savedDay;farm.Minute=735;dayTransitionStart=-10;
        art.Export(Path.Combine(output,"original-sprite-atlas.png"));
        using(var small=new Bitmap(960,640)){ClientSize=new(960,640);using var g=Graphics.FromImage(small);PaintBenchmark(g,10);small.Save(Path.Combine(output,"responsive-960.png"));}
        using(var large=new Bitmap(1920,1080)){ClientSize=new(1920,1080);using var g=Graphics.FromImage(large);PaintBenchmark(g,10);large.Save(Path.Combine(output,"responsive-1920.png"));}
        return checks;
    }
}
static class RenderDiagnostics {
    [DllImport("user32.dll")] static extern int GetGuiResources(IntPtr process,int flags);
    public static void Run(string output) {
        Directory.CreateDirectory(output);
        using var form=new GameForm(Path.Combine(output,"test-profile"));
        form.CreateControl();
        var farm=new Farm();form.TestWorld(farm);
        var cropKeys=Farm.Crops.Keys.ToArray();
        for(int i=0;i<24;i++){string crop=cropKeys[i%8];farm.Plots[i]=new(){Crop=crop,Growth=Farm.Crops[crop].Days,RipeDay=1,Moisture=i%3==0?15:70};}
        for(int i=0;i<3;i++){var p=farm.Join(Guid.NewGuid().ToString("N"),"เพื่อน "+(i+1));p.X=10+i;p.Y=10;}
        var results=new List<object>();
        foreach(var size in new[]{new Size(1280,800),new Size(960,640),new Size(1920,1080)})foreach(int quality in new[]{0,1,2})foreach(var scene in new[]{"day","storm-night"}) {
            form.ClientSize=size;form.SetQualityForTest(quality);
            farm.Weather=scene=="day"?"แดดอ่อน":"พายุ";farm.Minute=scene=="day"?720:1180;
            using var bitmap=new Bitmap(size.Width,size.Height);using var graphics=Graphics.FromImage(bitmap);
            for(int i=0;i<50;i++)form.PaintBenchmark(graphics,i/60d);
            bitmap.Save(Path.Combine(output,$"{scene}-{size.Width}-q{quality}.png"));
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
            using var process=Process.GetCurrentProcess();int gdiBefore=GetGuiResources(process.Handle,0);long memoryBefore=GC.GetTotalMemory(false);
            long allocationBefore=GC.GetAllocatedBytesForCurrentThread();var frames=new double[360];
            for(int i=0;i<frames.Length;i++){long start=Stopwatch.GetTimestamp();form.PaintBenchmark(graphics,1+i/60d);frames[i]=Stopwatch.GetElapsedTime(start).TotalMilliseconds;}
            long allocated=GC.GetAllocatedBytesForCurrentThread()-allocationBefore;
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();process.Refresh();Array.Sort(frames);
            results.Add(new{scene,quality,width=size.Width,height=size.Height,frames=frames.Length,meanMs=frames.Average(),p95Ms=frames[(int)(frames.Length*.95)],p99Ms=frames[(int)(frames.Length*.99)],renderCapacityFps=1000/frames.Average(),allocatedBytesPerFrame=allocated/frames.Length,managedDeltaBytes=GC.GetTotalMemory(false)-memoryBefore,privateBytes=process.PrivateMemorySize64,workingSetBytes=process.WorkingSet64,gdiBefore,gdiAfter=GetGuiResources(process.Handle,0)});
            Console.WriteLine($"{size.Width}x{size.Height} q{quality} {scene}: {frames.Average():F2} ms, p95 {frames[(int)(frames.Length*.95)]:F2}");
        }
        File.WriteAllText(Path.Combine(output,"render-benchmark.json"),JsonSerializer.Serialize(new{framework=RuntimeInformation.FrameworkDescription,os=RuntimeInformation.OSDescription,processorCount=Environment.ProcessorCount,method="Offscreen CPU render; includes animation and complete HUD, excludes display presentation and network I/O",results},new JsonSerializerOptions{WriteIndented=true}));
    }
}
