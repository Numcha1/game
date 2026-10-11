using Godot;

namespace SeasonsTogether;

public static class NpcAssets
{
    static Texture2D? characters,rooms,village;
    static PackedScene? actorScene;
    public static NpcActor CreateActor()=>(actorScene??=GD.Load<PackedScene>("res://Scenes/Village/NpcActor.tscn") ?? throw new InvalidDataException("NPC actor scene missing")).Instantiate<NpcActor>();
    static readonly Dictionary<string,SpriteFrames> frames=new();
    static readonly Dictionary<(string,int,int),AtlasTexture> poses=new();
    public static Texture2D Characters=>characters??=GD.Load<Texture2D>("res://Assets/Village/npc-atlas.png") ?? throw new InvalidDataException("NPC atlas missing");
    public static Texture2D Rooms=>rooms??=GD.Load<Texture2D>("res://Assets/Village/rooms-atlas.png") ?? throw new InvalidDataException("Village rooms missing");
    public static Texture2D Village=>village??=GD.Load<Texture2D>("res://Assets/Village/village.png") ?? throw new InvalidDataException("Village background missing");
    public static AtlasTexture Pose(string id,int row=0,int frame=0)
    {
        if(id is not ("mira" or "rowan"))throw new ArgumentException("Unsupported NPC",nameof(id));
        if(poses.TryGetValue((id,row,frame),out var cached))return cached;
        float w=Characters.GetWidth()/8f,h=Characters.GetHeight()/4f;
        int startX=(int)(((id=="mira"?0:4)+frame)*w),startY=(int)(row*h);
        int endX=(int)(((id=="mira"?0:4)+frame+1)*w),endY=(int)((row+1)*h);
        int left=endX,top=endY,right=startX,bottom=startY;
        using(var image=Characters.GetImage())for(int y=startY;y<endY;y++)for(int x=startX;x<endX;x++)
            if(image.GetPixel(x,y).A>.15f){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}
        if(left>right)throw new InvalidDataException("Empty NPC animation frame");
        int width=right-left+1,height=bottom-top+1;
        // Normalize transparent margins through atlas metadata; keep the original bitmap unchanged.
        return poses[(id,row,frame)]=new AtlasTexture{Atlas=Characters,Region=new Rect2(left,top,width,height),Margin=new Rect2((w-width)/2,h-height,w-width,h-height),FilterClip=true};
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
        int cell=id switch {"village.seed-shop"=>0,"village.workshop"=>1,"village.mira-home"=>2,"village.rowan-home"=>3,_=>throw new InvalidDataException("Unknown village room")};
        return new AtlasTexture{Atlas=Rooms,Region=new Rect2(cell%2*Rooms.GetWidth()/2,cell/2*Rooms.GetHeight()/2,Rooms.GetWidth()/2,Rooms.GetHeight()/2),FilterClip=true};
    }
}
