using Godot;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;

/// <summary>Visual room only; collision/authority live in Core, same player controller is retained.</summary>
public partial class InteriorRoom : Node2D
{
    GameReadModel read=null!;
    Sprite2D background=null!;
    Texture2D foregroundSource=null!;
    readonly List<Polygon2D> panes=new();
    readonly List<Sprite2D> lamps=new();
    int lastMinute=-1; bool lastLights;string lastWeather="";
    FarmScene owner=null!;
    AudioStreamPlayer ambience=null!;
    public virtual void Initialize(GameReadModel model,BuildingDefinition def,FarmScene scene)
    {
        read=model;owner=scene;background=GetNode<Sprite2D>("Background");
        foregroundSource=background.Texture;
        background.Texture=RoomPresentationAssets.Padded(def.Id,background.Texture);background.Scale=Vector2.One;background.Position=new Vector2(-16,0);
        foreach(var marker in new[]{("Spawn",def.Spawn),("Exit",def.Exit),("Bed",def.Bed),("Chest",def.Chest)})
            if(GetNodeOrNull<Marker2D>(marker.Item1) is not { } node || node.Position.DistanceTo(new((float)marker.Item2.X*32,(float)marker.Item2.Y*32))>.1f)
                throw new InvalidDataException("Interior marker does not match its building definition");
        if(GetNodeOrNull<Node2D>("Actors") is not {YSortEnabled:true})throw new InvalidDataException("Interior actors require Y sorting");
        var actors=GetNode<Node2D>("Actors");
        var logical=RoomPresentationAssets.Flat(def.Id,foregroundSource);
        RoomDepth.Add(actors,logical,"BedCluster",new(92,95),new(180,82),new(199,120),new(199,236),new(93,236));
        RoomDepth.Add(actors,logical,"StorageChest",new(570,101),new(650,97),new(655,161),new(570,167));
        RoomDepth.Add(actors,logical,"SittingArea",new(305,116),new(333,87),new(409,81),new(458,128),new(427,161),new(338,158));
        RoomDepth.Add(actors,logical,"PlantLeft",new(44,266),new(73,270),new(77,329),new(43,330));
        RoomDepth.Add(actors,logical,"PlantRight",new(691,266),new(731,278),new(733,370),new(692,370));
        foreach(int start in new[]{246,485}) for(int x=0;x<2;x++) for(int y=0;y<2;y++) {
            var p=new Vector2(start+x*24,32+y*26);var size=new Vector2(x==0?17:16,y==0?21:20);
            var pane=new Polygon2D {Polygon=new[]{p,p+new Vector2(size.X,0),p+size,p+new Vector2(0,size.Y)},ZIndex=-9};AddChild(pane);panes.Add(pane);
        }
        foreach(var pos in new[]{new Vector2(81,127),new Vector2(393,125)}) {
            var lamp=new Sprite2D {Texture=AssetCatalog.Shared.Glow,Position=pos,Modulate=new Color(1,.75f,.4f,.3f),ZIndex=5,Scale=new Vector2(.45f,.45f)};AddChild(lamp);lamps.Add(lamp);
        }
        var loop=(AudioStreamWav)AssetCatalog.Shared.Sound("interior-ambience").Duplicate();loop.LoopMode=AudioStreamWav.LoopModeEnum.Forward;loop.LoopBegin=0;loop.LoopEnd=loop.Data.Length/2;
        ambience=new AudioStreamPlayer {Stream=loop,Bus="Effects",VolumeDb=-80};AddChild(ambience);ambience.Play();
    }
    public override void _Process(double delta)
    {
        if(read==null || !Visible)return;
        int volume=owner.Display.EffectsVolume;
        ambience.VolumeDb=owner.DiagnosticAudioMuted || volume==0 ? -80 : Mathf.LinearToDb(volume/100f)-12;
        int minute=(int)read.World.Minute;bool lights=read.InteriorLightsOn;
        if(minute==lastMinute && lights==lastLights && lastWeather==read.World.Weather)return;lastMinute=minute;lastLights=lights;lastWeather=read.World.Weather;
        float night=Math.Clamp((minute-1020)/180f,0,1);
        owner.GetNode<CanvasModulate>("Ambient").Color=RoomLight.Ambient(read.World.Minute,lights,read.World.Weather);
        foreach(var pane in panes)pane.Color=new Color(.13f,.22f,.38f,night*.95f);
        foreach(var lamp in lamps)lamp.Visible=lights && minute>=1020;
    }
}
