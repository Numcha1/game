using Godot;
namespace SeasonsTogether;

/// <summary>Polls threaded resource requests; never waits on an unfinished loader in the game loop.</summary>
public sealed class FarmPreloader
{
    static readonly string[] Paths={
        "res://Scenes/Farm.tscn","res://Scenes/UI/ItemCard.tscn","res://Presentation/UI/FarmTheme.tres",
        "res://Assets/FarmTiles.tres","res://Assets/terrain.png","res://Assets/props.png","res://Assets/characters.png",
        "res://Assets/pond.png","res://Assets/shadow.png","res://Assets/stake.png","res://Assets/glow.png",
        "res://Assets/crop-growth.png","res://Assets/tool-icons.png","res://Assets/tool-poses.png",
        "res://Shaders/Wind.gdshader","res://Shaders/Water.gdshader",
        "res://Assets/Audio/click.wav","res://Assets/Audio/water.wav","res://Assets/Audio/harvest.wav","res://Assets/Audio/coin.wav","res://Assets/Audio/error.wav"
    };
    readonly Dictionary<string,Resource> loaded=new();
    public int Total=>Paths.Length;
    public int Completed=>loaded.Count;
    public string? Error { get; private set; }
    public PackedScene? Scene=>loaded.GetValueOrDefault(Paths[0]) as PackedScene;
    public static bool LastBootCompleted { get; private set; }
    public void Begin()
    {
        foreach(string path in Paths) {
            var result=ResourceLoader.LoadThreadedRequest(path);
            if(result!=Godot.Error.Ok) { Error="Resource request failed: "+path;return; }
        }
    }
    public bool Poll()
    {
        if(Error!=null)return false;
        foreach(string path in Paths) {
            if(loaded.ContainsKey(path))continue;
            switch(ResourceLoader.LoadThreadedGetStatus(path)) {
                case ResourceLoader.ThreadLoadStatus.Loaded:
                    var resource=ResourceLoader.LoadThreadedGet(path);
                    if(resource==null){Error="Resource missing: "+path;return false;}
                    loaded.Add(path,resource);break;
                case ResourceLoader.ThreadLoadStatus.Failed:
                case ResourceLoader.ThreadLoadStatus.InvalidResource:
                    Error="Resource loading failed: "+path;return false;
            }
        }
        bool complete=Completed==Total && Scene!=null;
        if(complete)LastBootCompleted=true;
        return complete;
    }
}
