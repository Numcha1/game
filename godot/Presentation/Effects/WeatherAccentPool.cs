using Godot;
using SeasonsTogether.Core.Application;
namespace SeasonsTogether;

/// <summary>Bounded lightweight seasonal accents, pond rings and ground splashes.</summary>
public partial class WeatherAccentPool : Node2D
{
    public GameReadModel Read { get; set; } = null!;
    public int Quality { get; set; } = 2;
    public bool ReducedMotion { get; set; }
    public bool PondRipplesEnabled { get; set; } = true;
    readonly Line2D[] rings = new Line2D[12], splashes = new Line2D[16], leaves = new Line2D[18];
    readonly Vector2[] leafPositions = new Vector2[18];
    double elapsed;
    int season = -1;
    public int VisibleRipples => rings.Count(r => r.Visible);
    public int VisibleSeasonal => leaves.Count(r => r.Visible);
    public override void _Ready()
    {
        var random=new Random(624);
        for(int i=0;i<rings.Length;i++) {
            rings[i]=Ring(new Color("c2e3d3"),6,2.5f);
            rings[i].Position=new Vector2(624+random.Next(-58,59),448+random.Next(-31,32));AddChild(rings[i]);
        }
        for(int i=0;i<splashes.Length;i++) {
            splashes[i]=Ring(new Color("b3d6d4"),2,.7f);
            splashes[i].Position=new Vector2(random.Next(40,760),random.Next(144,530));AddChild(splashes[i]);
        }
        for(int i=0;i<leaves.Length;i++) {
            leaves[i]=new Line2D { Width=1,Closed=true,Points=new Vector2[]{new(-2,0),new(0,-2),new(3,0),new(0,2)},Visible=false };
            leafPositions[i]=new Vector2(random.Next(800),random.Next(576));AddChild(leaves[i]);
        }
    }
    static Line2D Ring(Color color,float x,float y)
    {
        var points=new Vector2[12];
        for(int i=0;i<points.Length;i++){float a=Mathf.Tau*i/points.Length;points[i]=new Vector2(Mathf.Cos(a)*x,Mathf.Sin(a)*y);}
        return new Line2D {Points=points,Closed=true,Width=1,DefaultColor=color,Visible=false};
    }
    public override void _Process(double delta)
    {
        if(Read==null)return;elapsed+=delta;
        if(season!=Read.World.Season){season=Read.World.Season;foreach(var leaf in leaves)leaf.DefaultColor=new Color(new[]{"eabfc5","e6d681","d6a060","e0eceb"}[season]);}
        bool wet=Read.World.Weather is "ฝนตก" or "พายุ";
        int count=ReducedMotion?0:Quality switch{0=>4,1=>10,_=>18};
        for(int i=0;i<leaves.Length;i++) {
            var leaf=leaves[i];leaf.Visible=i<count && (season!=1 || Read.World.Minute>1080);
            if(!leaf.Visible)continue;
            leafPositions[i]+=new Vector2((float)Math.Sin(elapsed*.7+i)*6+3,season==3?8:season==1?-2:12)*(float)delta;
            if(leafPositions[i].Y>580)leafPositions[i].Y=-4;if(leafPositions[i].Y<-8)leafPositions[i].Y=578;
            if(leafPositions[i].X>804)leafPositions[i].X=-4;
            leaf.Position=leafPositions[i].Round();leaf.Modulate=new Color(1,1,1,season==1?.35f+(float)(Math.Sin(elapsed+i)+1)*.2f:.55f);
        }
        for(int i=0;i<rings.Length;i++) {
            float age=(float)((elapsed+i*.17)%1.8/1.8);var ring=rings[i];
            ring.Visible=PondRipplesEnabled && wet && !ReducedMotion && Quality>0 && (Quality==2 || i<6);
            if(!ring.Visible)continue;ring.Scale=Vector2.One*(.5f+age*1.8f);ring.Modulate=new Color(1,1,1,(1-age)*.45f);
        }
        for(int i=0;i<splashes.Length;i++) {
            float age=(float)((elapsed*1.4+i*.19)%1);var splash=splashes[i];
            splash.Visible=wet && !ReducedMotion && Quality>0 && age<.32f && (Quality==2 || i<8);
            if(!splash.Visible)continue;splash.Scale=Vector2.One*(.5f+age*4);splash.Modulate=new Color(1,1,1,(1-age/.32f)*.4f);
        }
    }
}
