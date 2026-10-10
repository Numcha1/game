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
        public bool Ring;
    }
    readonly Dictionary<string,AvatarPose> poses=new();
    readonly List<Spark> sparks=new();
    readonly Dictionary<string,SoundPlayer> sounds=new();
    readonly Random visualRandom=new(4319);
    long lastEffect;
    bool reducedMotion,soundEnabled=true,renderPreview;
    double? previewTime;
    string sessionId=>Farm.Identity(session);
    double VisualTime=>previewTime??elapsed.Elapsed.TotalSeconds;
    AvatarPose Avatar(string id){if(!poses.TryGetValue(id,out var a)){a=new();poses[id]=a;}return a;}
    void ResetVisuals(){poses.Clear();sparks.Clear();lastEffect=0;}
    void Animate(double dt) {
        sparks.RemoveAll(p=>VisualTime-p.Start>=p.Life);
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
        if(dayChanged){poses.Clear();sparks.Clear();}
        foreach(var effect in farm.Effects.Where(e=>e.Sequence>lastEffect))PlayEffect(effect);
        lastEffect=Math.Max(lastEffect,farm.EffectSequence);
    }
    void AddSpark(Spark p){if(sparks.Count>=384)sparks.RemoveAt(0);sparks.Add(p);}
    void PlayEffect(ActionEffect effect) {
        var pose=Avatar(effect.Actor);pose.Action=effect.Kind;pose.ActionStart=VisualTime;pose.ActionUntil=VisualTime+.72;
        PointF at=effect.Plot>=0&&effect.Plot<24?TilePoint(effect.Plot):new(32+(float)effect.X*35,112+(float)effect.Y*35);
        if(effect.Plot>=0&&effect.Plot<24){var t=Farm.Tile(effect.Plot);double dx=t.X-effect.X,dy=t.Y-effect.Y;pose.Facing=Math.Abs(dx)>Math.Abs(dy)?(dx<0?1:2):(dy<0?3:0);}
        at.X+=17;at.Y+=20;
        int count=reducedMotion?3:effect.Kind=="water"?18:12;
        Color color=effect.Kind switch {"water"=>Color.FromArgb(90,192,238),"harvest" or "sell"=>Gold,"treat"=>Color.FromArgb(143,223,150),_=>Color.FromArgb(177,129,77)};
        for(int i=0;i<count;i++)AddSpark(new(){Start=VisualTime,Life=.45+visualRandom.NextDouble()*.5,X=at.X,Y=at.Y,Vx=(visualRandom.NextDouble()-.5)*70,Vy=-20-visualRandom.NextDouble()*65,Color=color,Size=effect.Kind=="water"?3:4});
        AddSpark(new(){Start=VisualTime,Life=.75,X=at.X,Y=at.Y,Color=color,Ring=true});
        if(effect.Kind=="harvest")AddSpark(new(){Start=VisualTime,Life=.9,X=at.X,Y=at.Y-10,Crop=effect.Crop,Destination=new(32+(float)effect.X*35,112+(float)effect.Y*35-20),Color=Gold});
        string caption=effect.Kind switch {"plant"=>"ปลูกแล้ว", "water"=>"+น้ำ", "harvest"=>"เก็บเกี่ยว!", "fertilize"=>"+ดิน 30", "treat"=>"รักษาแล้ว", "stake"=>"ไม้ค้ำพร้อม", "clear"=>"ถอนแล้ว", "sell"=>"+"+effect.Amount+" เหรียญ", "rescue"=>"ทุน +18", "greenhouse"=>"หลังคาพร้อม!",_=>""};
        if(caption!="")AddSpark(new(){Start=VisualTime,Life=1.4,X=at.X-28,Y=at.Y-35,Vy=-22,Text=caption,Color=effect.Kind=="water"?Color.FromArgb(29,93,127):Moss});
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
    void DrawScene(Graphics g,Farm f) {
        var state=g.Save();g.SetClip(new RectangleF(32,112,875,630));
        Color ground=f.Season switch {1=>Color.FromArgb(151,179,96),2=>Color.FromArgb(177,157,100),3=>Color.FromArgb(207,222,217),_=>Color.FromArgb(153,182,114)};
        using(var gradient=new LinearGradientBrush(new RectangleF(32,112,875,630),ground,Color.FromArgb(Math.Max(0,ground.R-22),Math.Max(0,ground.G-17),Math.Max(0,ground.B-12)),80f))g.FillRectangle(gradient,32,112,875,630);
        // Deterministic tufts and tiny flowers. Only their sway changes over time.
        for(int i=0;i<210;i++){float x=47+(i*137%850),y=125+(i*97%595);float sway=reducedMotion?0:(float)Math.Sin(VisualTime*1.7+i)*2;Color blade=f.Season==3?Color.FromArgb(90,165,181,161):Color.FromArgb(75,75,113,55);Line(g,blade,1,x,y,x+sway-2,y-5);Line(g,blade,1,x+3,y,x+sway+4,y-4);if(i%13==0&&f.Season!=3){Oval(g,Color.FromArgb(240,221,145),x-2,y-8,4,4);Oval(g,Color.FromArgb(247,245,219),x+2,y-5,4,4);}}
        // Raised stone paths and wooden boundary fence.
        Round(g,Color.FromArgb(105,100,70,35),new(201,258,646,31));Round(g,Color.FromArgb(213,193,141),new(205,253,640,28));
        Round(g,Color.FromArgb(105,100,70,35),new(240,260,29,365));Round(g,Color.FromArgb(212,191,136),new(242,254,25,367));
        for(int i=0;i<29;i++){float x=216+i*22;Line(g,Color.FromArgb(160,179,156,107),1,x,256,x-3,277);}
        for(int i=0;i<16;i++){float y=274+i*22;Line(g,Color.FromArgb(140,174,149,102),1,244,y,266,y+3);}
        for(int i=0;i<24;i++){float x=37+i*37;Box(g,Color.FromArgb(90,79,62,40),x+2,130,6,34);Box(g,Color.FromArgb(220,193,138),x,125,6,34);Box(g,Color.FromArgb(250,228,176),x,125,2,33);}
        Box(g,Color.FromArgb(153,125,82),32,137,875,5);Box(g,Color.FromArgb(227,202,154),32,135,875,4);
        DrawPond(g,f);
        foreach(int i in Enumerable.Range(0,24))DrawSoil(g,f.Plots[i],i);
        var objects=new List<(float Depth,Action Draw)>();
        objects.Add((252,()=>RichHouse(g,102,147,1.5f,false)));
        objects.Add((239,()=>RichHouse(g,697,147,1.3f,true)));
        objects.Add((434,()=>DrawCrate(g,732,392)));
        for(int i=0;i<6;i++){float x=32+(i<3?i*2+1:i*2+6)*35;float y=112+14*35;objects.Add((y+68,()=>RichTree(g,x,y,1)));}
        // Shrubs create another depth layer without altering the navigation map.
        for(int i=0;i<3;i++){float x=55+i*81;objects.Add((411+i*3,()=>{Oval(g,Color.FromArgb(35,20,54,22),x+4,405,60,16);Oval(g,Color.FromArgb(92,129,59),x,382,58,30);Oval(g,Color.FromArgb(128,163,75),x+4,377,42,25);}));}
        for(int i=0;i<24;i++){int index=i;float depth=TilePoint(i).Y+29;objects.Add((depth,()=>DrawPlant(g,f.Plots[index],index)));}
        int n=0;foreach(var p in f.Players.Where(p=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-p.LastSeen<30000)) {
            var player=p;var a=Avatar(p.Id);if(!a.Initialized){a.X=p.Id==sessionId?px:p.X;a.Y=p.Id==sessionId?py:p.Y;a.Initialized=true;}
            Color shirt=new[]{Color.FromArgb(193,94,64),Color.FromArgb(75,126,164),Color.FromArgb(138,100,160),Color.FromArgb(218,173,65)}[n++%4];
            float x=32+(float)a.X*35,y=112+(float)a.Y*35;objects.Add((y+10,()=>DrawFarmer(g,x,y,shirt,a,player.Sleeping)));
        }
        foreach(var obj in objects.OrderBy(o=>o.Depth))obj.Draw();
        if(f.Greenhouse){var r=new RectangleF(308,314,217,148);Round(g,Color.FromArgb(22,205,245,237),r);Line(g,Color.FromArgb(135,203,229,223),3,r.X,r.Y,r.Right,r.Y);for(int i=0;i<4;i++)Line(g,Color.FromArgb(120,183,210,200),2,r.X+i*70,r.Y,r.X+i*70,r.Bottom);}
        // Time-of-day light affects the scene only, leaving the HUD legible.
        int shade=(int)Math.Clamp((f.Minute-990)/270*115,0,115);if(shade>0)Box(g,Color.FromArgb(shade,31,39,64),32,112,875,630);
        if(shade>30){Oval(g,Color.FromArgb(35,255,201,112),168,225,58,54);Oval(g,Color.FromArgb(22,255,201,112),715,214,70,68);}
        DrawAtmosphere(g,f);DrawSparks(g);
        foreach(var p in f.Players.Where(p=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-p.LastSeen<30000)){var a=Avatar(p.Id);float x=32+(float)a.X*35,y=112+(float)a.Y*35;float width=Math.Max(50,g.MeasureString(p.Name,F(12,true)).Width+12);Round(g,Color.FromArgb(190,248,241,220),new(x-width/2+2,y-59,width,20),5);TextAt(g,p.Name,x-width/2+8,y-57,12,Moss,true);}
        TextAt(g,"บ้าน · Space นอน",77,279,14,Moss,true);TextAt(g,"ร้าน · หลังคา 300",686,266,14,Moss,true);TextAt(g,"ลังขาย · E",714,445,14,Moss,true);
        g.Restore(state);
    }
    void DrawSoil(Graphics g,Plot plot,int index) {
        var t=TilePoint(index);float x=t.X,y=t.Y;
        Round(g,Color.FromArgb(70,53,44,21),new(x+3,y+5,33,33),4);
        Round(g,Color.FromArgb(97,65,37),new(x+1,y+2,33,33),4);
        Color soil=plot.Moisture>=25?Color.FromArgb(126,87,49):Color.FromArgb(172,132,86);
        Round(g,soil,new(x+1,y,33,30),4);Line(g,Color.FromArgb(196,153,95),1,x+5,y+2,x+28,y+2);
        for(int row=0;row<3;row++){Line(g,Color.FromArgb(70,63,43,26),2,x+5,y+8+row*8,x+29,y+8+row*8);Line(g,Color.FromArgb(55,231,187,112),1,x+5,y+10+row*8,x+29,y+10+row*8);}
        for(int j=0;j<3;j++)Oval(g,Color.FromArgb(65,237,197,142),x+6+(index*7+j*11)%21,y+3+(index*3+j*7)%22,2,2);
        if(plot.Moisture>80)Oval(g,Color.FromArgb(60,83,160,180),x+4,y+20,19,5);
    }
    void DrawPlant(Graphics g,Plot plot,int index) {
        var t=TilePoint(index);float x=t.X+17,y=t.Y+24;
        if(plot.Stake){Line(g,Color.FromArgb(105,71,41),3,x+13,y+6,x+13,y-28);Line(g,Color.FromArgb(209,176,110),1,x+12,y+5,x+12,y-28);}
        if(plot.Crop=="")return;
        float progress=(float)Math.Clamp(plot.Growth/Farm.Crops[plot.Crop].Days,0,1);
        float sway=reducedMotion?0:(float)Math.Sin(VisualTime*1.8+index*.71)*(1+progress);
        Oval(g,Color.FromArgb(65,47,60,30),x-12,y-1,26,8);
        var transform=g.Save();g.TranslateTransform(x+sway,y);float height=7+progress*18;
        Color leaf=plot.Disease?Color.FromArgb(157,144,53):Color.FromArgb(55,126,57);
        Line(g,Color.FromArgb(41,96,41),3,0,0,0,-height);
        Oval(g,leaf,-13,-height+2,14,7);Oval(g,Color.FromArgb(102,162,73),0,-height-4,13,7);
        Line(g,Color.FromArgb(178,196,103),1,-9,-height+5,0,-height+8);
        if(progress>.45) {Oval(g,leaf,-9,-height*.5f,11,6);Oval(g,leaf,0,-height*.6f,11,6);}
        if(plot.RipeDay>=0) {
            if(!reducedMotion&&index%3==0){float glint=(float)(.5+.5*Math.Sin(VisualTime*3+index));Oval(g,Color.FromArgb((int)(glint*100),255,232,124),-19,-25,38,31);}
            CropArt(g,plot.Crop,0,-7,1);
        } else if(progress>.65) {Oval(g,Color.FromArgb(242,211,142),-5,-height-3,5,5);}
        if(plot.Disease){TextAt(g,"!",9,-37,18,Color.FromArgb(183,47,33),true);}
        if(plot.Moisture<25){Poly(g,Color.FromArgb(107,193,232),new(-12,1),new(-16,7),new(-13,10),new(-9,7));}
        g.Restore(transform);
    }
    void CropArt(Graphics g,string crop,float x,float y,float s) {
        var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(s,s);
        Color dark=Color.FromArgb(58,97,43);
        switch(crop) {
            case "carrot": Poly(g,Color.FromArgb(235,140,45),new(-7,-7),new(7,-7),new(0,12));Line(g,Color.FromArgb(255,186,75),2,-3,-5,-1,4);for(int j=-1;j<=1;j++)Line(g,dark,2,0,-7,j*5,-15);break;
            case "corn": Oval(g,Color.FromArgb(229,183,43),-5,-14,12,25);for(int row=0;row<5;row++)for(int col=0;col<2;col++)Oval(g,Color.FromArgb(255,222,90),-3+col*5,-11+row*4,3,3);Poly(g,dark,new(-4,9),new(-13,-4),new(-10,9),new(0,13));Poly(g,Color.FromArgb(86,145,62),new(3,11),new(13,-7),new(10,12),new(0,13));break;
            case "pumpkin": Oval(g,Color.FromArgb(164,92,30),-13,-9,26,22);Oval(g,Color.FromArgb(232,145,47),-11,-10,22,22);Oval(g,Color.FromArgb(247,174,62),-5,-10,10,22);Line(g,dark,3,0,-9,2,-15);break;
            case "berry": Poly(g,Color.FromArgb(205,64,66),new(-9,-7),new(8,-7),new(6,4),new(0,12),new(-7,3));for(int j=0;j<5;j++)Oval(g,Color.FromArgb(247,212,130),-4+(j*5%9),-3+j*2,2,2);Poly(g,dark,new(-10,-8),new(0,-13),new(10,-8),new(0,-6));break;
            case "cabbage": Oval(g,Color.FromArgb(74,127,65),-13,-11,26,24);Oval(g,Color.FromArgb(145,186,96),-9,-10,18,21);Oval(g,Color.FromArgb(185,210,133),-6,-7,12,12);Line(g,Color.FromArgb(209,225,169),1,-6,8,0,-8);Line(g,Color.FromArgb(111,161,79),1,7,8,0,-8);break;
            case "tomato": Oval(g,Color.FromArgb(174,51,42),-11,-8,23,21);Oval(g,Color.FromArgb(224,81,56),-10,-9,20,19);Oval(g,Color.FromArgb(253,144,104),-6,-6,6,4);Poly(g,dark,new(-8,-9),new(0,-13),new(9,-8),new(0,-7));break;
            case "turnip": Oval(g,Color.FromArgb(231,229,208),-10,-9,20,21);Oval(g,Color.FromArgb(175,126,169),-10,-9,20,9);Line(g,Color.FromArgb(224,212,178),2,0,9,1,16);Line(g,dark,3,0,-10,-4,-17);Line(g,dark,3,0,-10,6,-17);break;
            default: Oval(g,Color.FromArgb(159,118,72),-11,-7,24,19);Oval(g,Color.FromArgb(198,160,99),-10,-9,21,18);Oval(g,Color.FromArgb(231,194,129),-6,-6,8,4);Oval(g,Color.FromArgb(118,89,55),6,1,3,2);break;
        }
        g.Restore(state);
    }
    void RichHouse(Graphics g,float x,float y,float s,bool shop) {
        var saved=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(s,s);
        Poly(g,Color.FromArgb(55,50,59,24),new(2,78),new(89,79),new(105,92),new(17,92));
        Box(g,Color.FromArgb(134,91,62),65,28,15,51);Box(g,Color.FromArgb(191,141,92),0,28,65,51);
        for(int row=0;row<6;row++)Line(g,Color.FromArgb(157,109,71),1,0,30+row*8,65,30+row*8);
        Box(g,Color.FromArgb(220,189,130),0,28,4,51);Box(g,Color.FromArgb(220,189,130),61,28,4,51);
        Poly(g,Color.FromArgb(37,66,46),new(-9,31),new(39,-4),new(89,30),new(72,37),new(39,9),new(-9,37));
        Poly(g,shop?Color.FromArgb(98,127,99):Color.FromArgb(83,119,99),new(-9,30),new(39,-4),new(70,29));
        for(int row=0;row<4;row++)Line(g,Color.FromArgb(143,166,128),1,3+row*9,21-row*6,56+row*3,21-row*6);
        Box(g,Color.FromArgb(124,79,57),55,-5,9,17);Box(g,Color.FromArgb(198,157,116),54,-6,11,4);
        if(!reducedMotion)for(int i=0;i<3;i++){float phase=(float)((VisualTime*.3+i*.3)%1);Oval(g,Color.FromArgb((int)((1-phase)*60),247,240,215),54+phase*10,-9-phase*25,7+phase*10,6+phase*9);}
        Box(g,Color.FromArgb(85,63,42),9,39,22,21);Box(g,Color.FromArgb(247,199,95),11,41,18,17);Line(g,Color.FromArgb(238,215,164),2,20,41,20,58);Line(g,Color.FromArgb(238,215,164),2,11,49,29,49);
        Box(g,Color.FromArgb(109,72,46),39,47,19,32);Box(g,Color.FromArgb(163,114,69),41,48,15,29);Oval(g,Gold,52,63,2,2);
        Box(g,Color.FromArgb(119,106,73),35,80,28,5);Box(g,Color.FromArgb(193,178,138),33,84,32,4);
        if(shop){Box(g,Color.FromArgb(91,127,93),0,31,65,7);for(int i=0;i<8;i+=2)Box(g,Color.FromArgb(242,229,185),i*8,31,8,7);TextAt(g,"SHOP",7,64,8,Cream,true);}
        else{Box(g,Color.FromArgb(137,92,55),4,65,26,7);for(int i=0;i<4;i++){Oval(g,Color.FromArgb(98,146,61),7+i*6,60,6,7);Oval(g,Color.FromArgb(229,158,122),9+i*6,59,3,3);}}
        g.Restore(saved);
    }
    void RichTree(Graphics g,float x,float y,float s) {
        var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(s,s);float sway=reducedMotion?0:(float)Math.Sin(VisualTime*.95+x)*1.8f;
        Oval(g,Color.FromArgb(43,38,61,24),6,66,60,17);
        Box(g,Color.FromArgb(109,76,48),23,35,11,41);Box(g,Color.FromArgb(161,116,67),24,35,4,40);Line(g,Color.FromArgb(82,62,39),2,30,46,38,34);
        int season=world?.Season??0;Color deep=season==2?Color.FromArgb(137,103,55):Color.FromArgb(59,109,63);Color light=season==2?Color.FromArgb(208,155,73):season==3?Color.FromArgb(186,205,168):Color.FromArgb(132,172,83);
        Oval(g,deep,sway-1,17,58,50);Oval(g,deep,sway+7,1,46,54);Oval(g,Color.FromArgb(94,146,74),sway+1,7,41,44);Oval(g,light,sway+8,-2,35,37);Oval(g,Color.FromArgb(65,237,244,185),sway+11,0,25,18);
        for(int i=0;i<5;i++)Oval(g,season==3?Color.FromArgb(230,239,229):Gold,sway+10+(i*13%34),17+(i*17%31),4,4);
        if(season==3){Oval(g,Color.FromArgb(236,244,235),sway+9,-3,32,9);}
        g.Restore(state);
    }
    void DrawCrate(Graphics g,float x,float y) {
        Poly(g,Color.FromArgb(65,47,51,26),new(x,y+38),new(x+43,y+40),new(x+52,y+46),new(x+5,y+46));
        Box(g,Color.FromArgb(131,87,45),x,y+6,35,32);Poly(g,Color.FromArgb(104,68,39),new(x+35,y+6),new(x+43,y+1),new(x+43,y+32),new(x+35,y+38));
        Poly(g,Color.FromArgb(200,157,91),new(x,y+6),new(x+8,y),new(x+43,y+1),new(x+35,y+6));
        for(int i=0;i<3;i++)Line(g,Color.FromArgb(202,161,101),2,x+2,y+10+i*10,x+33,y+10+i*10);
        Box(g,Color.FromArgb(227,210,163),x+9,y+15,18,12);TextAt(g,"SELL",x+10,y+16,7,Moss,true);
    }
    void DrawPond(Graphics g,Farm f) {
        Oval(g,Color.FromArgb(97,114,75),619,526,221,143);Oval(g,Color.FromArgb(199,187,143),622,522,219,141);
        Oval(g,f.Season==3?Color.FromArgb(149,199,202):Color.FromArgb(77,144,162),629,526,209,131);
        Oval(g,Color.FromArgb(72,152,207,199),657,536,150,75);
        for(int i=0;i<6;i++){float phase=reducedMotion ? 0.35f : (float)((VisualTime*.15+i*.167)%1);using var pen=new Pen(Color.FromArgb((int)((1-phase)*100),205,240,223),1);float x=667+i*21%115,y=555+i*19%55;g.DrawEllipse(pen,x-phase*20,y-phase*7,18+phase*40,5+phase*15);}
        for(int i=0;i<6;i++){float x=630+i*30,y=645+(i%2)*4;Oval(g,Color.FromArgb(148,148,112),x,y,13,8);Line(g,Color.FromArgb(79,122,74),2,x,y,x-2,y-15);}
        // A pair of ducks bob and paddle independently of player actions.
        for(int i=0;i<2;i++){float x=690+i*45+(reducedMotion?0:(float)Math.Sin(VisualTime*.35+i)*14),y=585+i*21+(reducedMotion?0:(float)Math.Sin(VisualTime*1.2+i));Oval(g,Color.FromArgb(80,225,248,230),x-3,y+5,27,6);Oval(g,Color.FromArgb(237,232,197),x,y-4,20,11);Oval(g,Color.FromArgb(56,111,73),x+13,y-10,9,10);Poly(g,Gold,new(x+21,y-6),new(x+28,y-3),new(x+21,y-2));}
    }
    void DrawFarmer(Graphics g,float x,float y,Color shirt,AvatarPose a,bool sleeping) {
        float step=reducedMotion?0:(float)Math.Sin(a.Phase)*Math.Min(1,(float)a.Speed)*4;
        float bob=reducedMotion?0:(float)Math.Abs(Math.Sin(a.Phase))*(a.Speed>.1?1.5f:0);
        double progress=a.ActionUntil>VisualTime?Math.Clamp((VisualTime-a.ActionStart)/.72,0,1):0;
        float swing=(float)Math.Sin(progress*Math.PI),bend=progress>0?swing*6:0;
        Oval(g,Color.FromArgb(65,33,46,21),x-13,y+4,29,10);
        var state=g.Save();g.TranslateTransform(x,y-bob+bend);
        float left=a.Facing==1?-3:a.Facing==2?3:0;
        Line(g,Color.FromArgb(47,64,68),6,-5,-6,-5-step*.5f,6+step);Line(g,Color.FromArgb(47,64,68),6,5,-6,5+step*.5f,6-step);
        Round(g,Color.FromArgb(85,62,45),new(-10-step*.5f,4+step,10,5),2);Round(g,Color.FromArgb(85,62,45),new(1+step*.5f,4-step,10,5),2);
        Line(g,Color.FromArgb(230,177,124),5,-10,-21,-13,-10-step);Line(g,Color.FromArgb(230,177,124),5,11,-21,14+left+swing*8,-10+step-swing*10);
        Round(g,Color.FromArgb(Math.Max(0,shirt.R-27),Math.Max(0,shirt.G-22),Math.Max(0,shirt.B-19)),new(-10,-25,21,23),4);
        Round(g,shirt,new(-10,-26,18,20),4);Box(g,Color.FromArgb(200,194,134),-6,-24,3,17);Box(g,Color.FromArgb(200,194,134),4,-24,3,17);Box(g,Color.FromArgb(172,165,104),-6,-14,13,8);
        Oval(g,Color.FromArgb(151,98,57),-8+left,-42,18,19);Oval(g,Color.FromArgb(235,191,141),-7+left,-39,17,16);
        if(a.Facing!=3){Oval(g,Color.FromArgb(58,52,44),-2+left,-33,2,3);if(a.Facing==0)Oval(g,Color.FromArgb(58,52,44),5,-33,2,3);Line(g,Color.FromArgb(179,108,74),1,1+left,-27,4+left,-27);}
        else Box(g,Color.FromArgb(113,73,44),-6,-38,16,11);
        Oval(g,Color.FromArgb(157,111,49),-15,-44,33,8);Oval(g,Color.FromArgb(226,187,103),-15,-46,33,8);Round(g,Color.FromArgb(233,199,119),new(-7,-54,19,12),4);Box(g,Color.FromArgb(143,110,56),-7,-45,19,3);
        if(progress>0){
            var arm=g.Save();g.TranslateTransform(17+left,-12-swing*10);g.RotateTransform((a.Facing==1?-1:1)*(swing*55-15));
            if(a.Action=="water"){Round(g,Color.FromArgb(106,172,181),new(-5,-4,14,11),3);Line(g,Color.FromArgb(63,122,137),3,8,-2,17,-7);using var pen=new Pen(Color.FromArgb(176,220,223),2);g.DrawArc(pen,-6,-10,12,10,180,170);}
            else if(a.Action=="plant"||a.Action=="clear"||a.Action=="fertilize"){Line(g,Color.FromArgb(169,125,66),3,-2,-10,2,12);Line(g,Color.FromArgb(135,153,150),5,-4,10,10,10);}
            else if(a.Action=="harvest"){Round(g,Color.FromArgb(182,137,77),new(-8,-3,20,13),3);Line(g,Gold,2,-7,1,10,1);}
            else if(a.Action=="treat"){Round(g,Color.FromArgb(150,196,132),new(-3,-7,9,15),2);Box(g,Cream,-1,-4,5,8);}
            g.Restore(arm);
        }
        if(sleeping)TextAt(g,"z Z",7,-66,15,Moss,true);
        g.Restore(state);
    }
    void DrawAtmosphere(Graphics g,Farm f) {
        if(reducedMotion)return;
        if(f.Weather is "ฝนตก" or "พายุ") {
            Box(g,Color.FromArgb(f.Weather=="พายุ"?38:16,35,65,90),32,112,875,630);
            for(int i=0;i<90;i++){float x=32+(float)((i*137+VisualTime*48)%875),y=112+(float)((i*83+VisualTime*390)%630);Line(g,Color.FromArgb(100,192,222,228),1,x,y,x-5,y+13);if(i%11==0){using var pen=new Pen(Color.FromArgb(65,206,232,226),1);g.DrawEllipse(pen,x-3,y+13,8,3);}}
        } else if(f.Season==3){for(int i=0;i<55;i++){float x=32+(float)((i*137+Math.Sin(VisualTime+i)*7)%875),y=112+(float)((i*83+VisualTime*24)%630);Oval(g,Color.FromArgb(190,252,255,247),x,y,3,3);}}
        else if(f.Season==2){for(int i=0;i<10;i++){float x=32+(float)((i*131+VisualTime*20)%875),y=112+(float)((i*83+VisualTime*31)%630);var st=g.Save();g.TranslateTransform(x,y);g.RotateTransform((float)(VisualTime*70+i*19));Oval(g,Color.FromArgb(190,195,132,53),0,0,7,3);g.Restore(st);}}
        else{for(int i=0;i<3;i++){float x=80+i*200+(float)Math.Sin(VisualTime*.6+i)*36,y=410+i*23+(float)Math.Cos(VisualTime*.8+i)*24;float flap=(float)Math.Abs(Math.Sin(VisualTime*9+i))*5;Oval(g,Color.FromArgb(220,240,196,94),x-flap,y-3,flap+1,6);Oval(g,Color.FromArgb(220,241,167,116),x,y-3,flap+1,6);Line(g,Moss,1,x,y-3,x,y+3);}}
    }
    void DrawSparks(Graphics g) {
        foreach(var p in sparks){double age=VisualTime-p.Start,t=Math.Clamp(age/p.Life,0,1);int alpha=(int)((1-t)*230);Color color=Color.FromArgb(alpha,p.Color);
            float x=(float)(p.X+p.Vx*age),y=(float)(p.Y+p.Vy*age+(p.Text!=""?0:55*age*age));
            if(p.Ring){using var pen=new Pen(color,2);g.DrawEllipse(pen,(float)p.X-(float)t*22,(float)p.Y-(float)t*9,4+(float)t*44,2+(float)t*18);}
            else if(p.Text!="")TextAt(g,p.Text,x,y,14,color,true);
            else if(p.Crop!=""&&p.Destination is PointF destination){float u=(float)t;x=(float)p.X+(destination.X-(float)p.X)*u;y=(float)p.Y+(destination.Y-(float)p.Y)*u-(float)Math.Sin(u*Math.PI)*48;CropArt(g,p.Crop,x,y,(float)(1-t*.55));}
            else Oval(g,color,x,y,p.Size,p.Size);
        }
    }
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
