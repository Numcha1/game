using Godot;

namespace SeasonsTogether;

public sealed class InteriorSceneLoader
{
    readonly Dictionary<string,PackedScene> cache=new();
    public int CachedCount=>cache.Count;
    public async Task<PackedScene> Load(string path,Func<Task> nextFrame,CancellationToken cancellation)
    {
        if(cache.TryGetValue(path,out var ready))return ready;
        if(!ResourceLoader.Exists(path,"PackedScene"))throw new InvalidDataException("ไม่พบฉากภายในอาคาร");
        if(ResourceLoader.LoadThreadedRequest(path,"PackedScene")!=Error.Ok)throw new InvalidDataException("โหลดฉากภายในไม่ได้");
        var started=System.Diagnostics.Stopwatch.StartNew();
        while(started.Elapsed<TimeSpan.FromSeconds(10)) {
            cancellation.ThrowIfCancellationRequested();
            var status=ResourceLoader.LoadThreadedGetStatus(path);
            if(status==ResourceLoader.ThreadLoadStatus.Loaded) {
                if(ResourceLoader.LoadThreadedGet(path) is not PackedScene scene)throw new InvalidDataException("รูปแบบฉากภายในไม่ถูกต้อง");
                return cache[path]=scene;
            }
            if(status is ResourceLoader.ThreadLoadStatus.Failed or ResourceLoader.ThreadLoadStatus.InvalidResource)throw new InvalidDataException("โหลดฉากภายในไม่สำเร็จ");
            await nextFrame();
        }
        throw new IOException("โหลดฉากภายในนานเกินไป");
    }
}
