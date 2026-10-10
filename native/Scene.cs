using System.Drawing.Drawing2D;
using System.Media;

namespace SuanPleng;

public sealed partial class GameForm {
    sealed class AvatarPose {
        public double X,Y,Phase,Speed,ActionUntil,ActionStart;
        public int Facing;
        public string Action="";
        public bool Initialized;
    }
    sealed class Spark {
        public double Start,Life,X,Y,Vx,Vy;
        public Color Color;
        public float Size;
        public string Text="",Crop="";
        public PointF? Destination;
        public bool Ring,Coin;
    }
    readonly Dictionary<string,AvatarPose> poses=new();
    readonly List<Spark> sparks=new();
    readonly Dictionary<string,SoundPlayer> sounds=new();
    readonly Random visualRandom=new(4319);
    long lastEffect;
    bool reducedMotion,soundEnabled=true,renderPreview;
    double? previewTime;
    string sessionId=>publicPlayerId;
    double VisualTime=>previewTime??elapsed.Elapsed.TotalSeconds;
    AvatarPose Avatar(string id){if(!poses.TryGetValue(id,out var a)){a=new();poses[id]=a;}return a;}
    void ClearSparks(){foreach(var p in sparks)ReturnSpark(p);sparks.Clear();}
    void ResetVisuals(){poses.Clear();ClearSparks();lastEffect=0;}
    void Animate(double dt) {
        for(int i=sparks.Count-1;i>=0;i--)if(VisualTime-sparks[i].Start>=sparks[i].Life){ReturnSpark(sparks[i]);sparks.RemoveAt(i);}
        UpdatePresentation(dt);
        if(world==null)return;
        foreach(var p in world.Players) {
            var a=Avatar(p.Id);double x=p.Id==sessionId?px:p.X,y=p.Id==sessionId?py:p.Y;
            if(!a.Initialized){a.X=x;a.Y=y;a.Initialized=true;}
            double dx=x-a.X,dy=y-a.Y;
            double nx=p.Id==sessionId?x:a.X+dx*(1-Math.Exp(-14*dt)),ny=p.Id==sessionId?y:a.Y+dy*(1-Math.Exp(-14*dt));
            double speed=Math.Sqrt((nx-a.X)*(nx-a.X)+(ny-a.Y)*(ny-a.Y))/Math.Max(dt,.001);
            a.Speed=Math.Min(speed,5);if(speed>.06){a.Facing=Math.Abs(dx)>Math.Abs(dy)?(dx<0?1:2):(dy<0?3:0);a.Phase+=speed*dt*3.4;}
            a.X=nx;a.Y=ny;
        }
        foreach(string id in poses.Keys.Where(id=>!world.Players.Any(p=>p.Id==id)).ToArray())poses.Remove(id);
    }
    void ReceiveEffects(Farm farm,bool initial,bool dayChanged) {
        if(initial){lastEffect=farm.EffectSequence;return;}
        if(dayChanged){poses.Clear();ClearSparks();}
        foreach(var effect in farm.Effects.Where(e=>e.Sequence>lastEffect))PlayEffect(effect);
        lastEffect=Math.Max(lastEffect,farm.EffectSequence);
    }
    void ReturnSpark(Spark p){if(sparkPool.Count<384)sparkPool.Push(p);}
    void AddSpark(Spark p){if(sparks.Count>=384){ReturnSpark(sparks[0]);sparks.RemoveAt(0);}sparks.Add(p);}
    Spark RentSpark(double x,double y,double life,Color color,double vx=0,double vy=0,float size=3) {
        var p=sparkPool.Count>0?sparkPool.Pop():new Spark();
        p.Start=VisualTime;p.Life=life;p.X=x;p.Y=y;p.Vx=vx;p.Vy=vy;p.Color=color;p.Size=size;
        p.Text="";p.Crop="";p.Destination=null;p.Ring=false;p.Coin=false;return p;
    }
    void PlayEffect(ActionEffect effect) {
        var pose=Avatar(effect.Actor);pose.Action=effect.Kind;pose.ActionStart=VisualTime;pose.ActionUntil=VisualTime+.72;
        PointF at=effect.Plot>=0&&effect.Plot<24?TilePoint(effect.Plot):new(32+(float)effect.X*35,112+(float)effect.Y*35);
        if(effect.Plot>=0&&effect.Plot<24){var t=Farm.Tile(effect.Plot);double dx=t.X-effect.X,dy=t.Y-effect.Y;pose.Facing=Math.Abs(dx)>Math.Abs(dy)?(dx<0?1:2):(dy<0?3:0);}
        at.X+=17;at.Y+=20;
        int count=reducedMotion?3:quality==0?5:effect.Kind=="water"?18:12;
        Color color=effect.Kind switch {"water"=>Color.FromArgb(145,208,215),"harvest" or "sell"=>Gold,"treat"=>Color.FromArgb(143,193,121),_=>Color.FromArgb(176,135,90)};
        for(int i=0;i<count;i++){var p=RentSpark(at.X,at.Y,.45+visualRandom.NextDouble()*.5,color,(visualRandom.NextDouble()-.5)*70,-20-visualRandom.NextDouble()*65,effect.Kind=="water"?2:3);p.Coin=effect.Kind=="sell";AddSpark(p);}
        var ring=RentSpark(at.X,at.Y,.75,color);ring.Ring=true;AddSpark(ring);
        if(effect.Kind=="harvest"){var p=RentSpark(at.X,at.Y-10,.9,Gold);p.Crop=effect.Crop;p.Destination=new(32+(float)effect.X*35,112+(float)effect.Y*35-20);AddSpark(p);}
        string caption=effect.Kind switch {"plant"=>"ปลูกแล้ว", "water"=>"+น้ำ", "harvest"=>"เก็บเกี่ยว!", "fertilize"=>"+ดิน 30", "treat"=>"รักษาแล้ว", "stake"=>"ไม้ค้ำพร้อม", "clear"=>"ถอนแล้ว", "sell"=>"+"+effect.Amount+" เหรียญ", "rescue"=>"ทุน +18", "greenhouse"=>"หลังคาพร้อม!",_=>""};
        if(caption!=""){var p=RentSpark(at.X-28,at.Y-35,1.4,effect.Kind=="water"?Color.FromArgb(54,102,115):Moss,vy:-22);p.Text=caption;AddSpark(p);}
        if(soundEnabled&&!renderPreview&&effect.Actor==sessionId)PlaySound(effect.Kind);
    }
    void PlaySound(string kind) {
        try {
            if(!sounds.TryGetValue(kind,out var sound)) {
                const int rate=16000;int length=kind=="harvest"||kind=="sell"?4200:2200;
                var stream=new MemoryStream();var writer=new BinaryWriter(stream);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(length*2);
                for(int i=0;i<length;i++){double t=i/(double)rate,progress=i/(double)length;double frequency=kind switch {"water"=>900-progress*600,"harvest" or "sell"=>520+progress*500,"plant"=>180+progress*120,_=>330};double amplitude=Math.Sin(t*frequency*2*Math.PI)*Math.Sin(progress*Math.PI)*.09;writer.Write((short)(amplitude*short.MaxValue));}
                writer.Flush();stream.Position=0;sound=new SoundPlayer(stream);sound.Load();sounds[kind]=sound;
            }
            sound.Play();
        }catch(Exception e)when(e is InvalidOperationException or IOException or System.ComponentModel.Win32Exception){/* Audio is optional. */}
    }
    static PointF TilePoint(int index){var t=Farm.Tile(index);return new(32+t.X*35,112+t.Y*35);}
    void Line(Graphics g,Color color,float width,float x1,float y1,float x2,float y2){using var pen=new Pen(color,width){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(pen,x1,y1,x2,y2);}
    void Poly(Graphics g,Color color,params PointF[] points){using var brush=B(color);g.FillPolygon(brush,points);}
    void Round(Graphics g,Color color,RectangleF r,float radius=7){using var path=new GraphicsPath();float d=radius*2;path.AddArc(r.X,r.Y,d,d,180,90);path.AddArc(r.Right-d,r.Y,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.X,r.Bottom-d,d,d,90,90);path.CloseFigure();using var b=B(color);g.FillPath(b,path);}
    public void AnimationSmoke(string folder) {
        renderPreview=true;previewTime=10;Animate(.016);
        var historical=new Farm{EffectSequence=7,Effects=new(){new(7,sessionId,"water",0,"carrot",0,8,6)}};
        ResetVisuals();ReceiveEffects(historical,true,false);if(sparks.Count!=0)throw new InvalidOperationException("Historical effects replayed on join");
        historical.EffectSequence=8;historical.Effects.Add(new(8,sessionId,"water",0,"carrot",0,8,6));ReceiveEffects(historical,false,false);
        int received=sparks.Count;ReceiveEffects(historical,false,false);if(received==0||sparks.Count!=received)throw new InvalidOperationException("Snapshot effects duplicated");ResetVisuals();
        var effect=new ActionEffect(1,sessionId,"water",0,"carrot",0,8,6);PlayEffect(effect);
        int initial=sparks.Count;if(initial==0)throw new InvalidOperationException("No action particles created");
        for(int i=0;i<3;i++){previewTime=10+i*.25;Avatar(sessionId).Phase=i*1.5;Avatar(sessionId).Speed=3;using var image=RenderForTest();image.Save(Path.Combine(folder,$"animation-{i}.png"));}
        var first=File.ReadAllBytes(Path.Combine(folder,"animation-0.png"));var second=File.ReadAllBytes(Path.Combine(folder,"animation-1.png"));if(first.SequenceEqual(second))throw new InvalidOperationException("Animation frames are identical");
        previewTime=13;Animate(.016);if(sparks.Count!=0)throw new InvalidOperationException("Animation particles did not expire");
        for(int i=0;i<80;i++)PlayEffect(effect);if(sparks.Count>384)throw new InvalidOperationException("Unbounded particles");previewTime=16;Animate(.016);if(sparks.Count!=0)throw new InvalidOperationException("Burst particles did not expire");
        previewTime=null;renderPreview=false;
    }
}
