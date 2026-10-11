using Godot;

namespace SeasonsTogether;

public static class NpcAssets
{
    static Texture2D? characters,rooms,village,compositionRooms;
    static readonly Dictionary<string,AtlasTexture> roomViews=new();
    public static Texture2D CompositionRooms=>compositionRooms??=GD.Load<Texture2D>("res://Assets/Village/rooms-composition.png") ?? throw new InvalidDataException("Room composition asset missing");
    static PackedScene? actorScene;
    public static NpcActor CreateActor()=>(actorScene??=GD.Load<PackedScene>("res://Scenes/Village/NpcActor.tscn") ?? throw new InvalidDataException("NPC actor scene missing")).Instantiate<NpcActor>();
    static readonly Dictionary<string,SpriteFrames> frames=new();
    static readonly Dictionary<(string,int,int),AtlasTexture> poses=new();
    public static Texture2D Characters=>characters??=GD.Load<Texture2D>("res://Assets/Village/npc-logical.png") ?? throw new InvalidDataException("NPC atlas missing");
    public static Texture2D Rooms=>rooms??=GD.Load<Texture2D>("res://Assets/Village/rooms-atlas.png") ?? throw new InvalidDataException("Village rooms missing");
    public static Texture2D Village=>village??=GD.Load<Texture2D>("res://Assets/Village/village.png") ?? throw new InvalidDataException("Village background missing");
    public static AtlasTexture Pose(string id,int row=0,int frame=0)
    {
        if(id is not ("mira" or "rowan"))throw new ArgumentException("Unsupported NPC",nameof(id));
        if(poses.TryGetValue((id,row,frame),out var cached))return cached;
        return poses[(id,row,frame)]=new AtlasTexture{Atlas=Characters,Region=new Rect2(((id=="mira"?0:4)+frame)*40,row*56,40,56),FilterClip=true};
    }
    public static SpriteFrames Frames(string id)
    {
        if(frames.TryGetValue(id,out var cached))return cached;
        var result=new SpriteFrames();result.RemoveAnimation("default");
        foreach(var row in new[]{("down",0),("left",1),("right",2),("up",3)}) {
            result.AddAnimation(row.Item1);result.SetAnimationSpeed(row.Item1,6);result.SetAnimationLoopMode(row.Item1,SpriteFrames.LoopMode.Linear);
            for(int i=0;i<4;i++)result.AddFrame(row.Item1,Pose(id,row.Item2,i));
        }
        return frames[id]=result;
    }
    public static AtlasTexture Room(string id)
    {
        if(roomViews.TryGetValue(id,out var cached))return cached;
        var atlas=CompositionRooms;
        int cell=id switch {"village.seed-shop"=>0,"village.workshop"=>1,"village.mira-home"=>2,"village.rowan-home"=>3,_=>throw new InvalidDataException("Unknown village room")};
        return roomViews[id]=new AtlasTexture{Atlas=atlas,Region=new Rect2(cell%2*atlas.GetWidth()/2,cell/2*atlas.GetHeight()/2,atlas.GetWidth()/2,atlas.GetHeight()/2),FilterClip=true};
    }
}
