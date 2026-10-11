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
    int lastMinute=-1; bool lastLights;
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
        // Reuse atlas regions from the original background for foreground occlusion; no extra bitmap copies.
        AddForeground(actors,new Rect2(178,158,222,318));
        AddForeground(actors,new Rect2(1140,188,174,146));
        AddForeground(actors,new Rect2(606,158,312,156));
        AddForeground(actors,new Rect2(78,534,80,132));
        AddForeground(actors,new Rect2(1380,536,82,212));
        AddForeground(actors,new Rect2(100,188,94,126));
        AddForeground(actors,new Rect2(1320,90,122,210));
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
    void AddForeground(Node2D actors,Rect2 region)
    {
        var texture=new AtlasTexture {Atlas=foregroundSource,Region=region,FilterClip=true};
        actors.AddChild(new Sprite2D {Texture=texture,Scale=Vector2.One*.5f,Position=new Vector2(region.Position.X+region.Size.X/2,region.End.Y)*.5f,Offset=new Vector2(0,-region.Size.Y/2)});
    }
    public override void _Process(double delta)
    {
        if(read==null || !Visible)return;
        int volume=owner.Display.EffectsVolume;
        ambience.VolumeDb=owner.DiagnosticAudioMuted || volume==0 ? -80 : Mathf.LinearToDb(volume/100f)-12;
        int minute=(int)read.World.Minute;bool lights=read.InteriorLightsOn;
        if(minute==lastMinute && lights==lastLights)return;lastMinute=minute;lastLights=lights;
        float night=Math.Clamp((minute-1020)/180f,0,1);
        owner.GetNode<CanvasModulate>("Ambient").Color=lights?new Color(1,.98f,.94f):Colors.White.Lerp(new Color(.55f,.62f,.76f),night);
        foreach(var pane in panes)pane.Color=new Color(.13f,.22f,.38f,night*.95f);
        foreach(var lamp in lamps)lamp.Visible=lights && minute>=1020;
    }
}
