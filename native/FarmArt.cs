using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SuanPleng;

// Original programmatic pixel artwork. Reference screenshots are never loaded or copied.
// Everything is rasterized once into an atlas; the frame loop only blits cached sprites.
sealed class FarmArt : IDisposable {
    public readonly Bitmap Atlas=new(2048,1024,PixelFormat.Format32bppPArgb);
    public readonly Bitmap[] Ground=new Bitmap[4];
    public readonly TextureBrush[] OuterGrass=new TextureBrush[4];
    readonly Bitmap[] grassTiles=new Bitmap[4];
    public readonly Rectangle[,] Trees=new Rectangle[4,3];
    public readonly Rectangle[] Houses=new Rectangle[2],Soils=new Rectangle[3],Icons=new Rectangle[12];
    public readonly Rectangle[] WeatherIcons=new Rectangle[3];
    readonly ImageAttributes[] fade=new ImageAttributes[65];
    public readonly Rectangle[,] Crops=new Rectangle[8,4];
    public readonly Rectangle[,,] Farmers=new Rectangle[4,4,8];
    public Rectangle Crate,Reeds,Rock,Bush,Trunk,Shadow,Glow,Barrel,Mailbox;
    int cursorX=2,cursorY=2,rowHeight;
    static readonly Color Ink=C("3e4036"),Wood=C("9f6843"),LightWood=C("dca774"),Cream=C("f4e6bd");
    static Color C(string hex)=>Color.FromArgb(Convert.ToInt32(hex[..2],16),Convert.ToInt32(hex[2..4],16),Convert.ToInt32(hex[4..6],16));
    static Color Shade(Color c,int delta)=>Color.FromArgb(Math.Clamp(c.R+delta,0,255),Math.Clamp(c.G+delta,0,255),Math.Clamp(c.B+delta,0,255));
    static void R(Graphics g,Color c,float x,float y,float w,float h){using var b=new SolidBrush(c);g.FillRectangle(b,x,y,w,h);}
    static void E(Graphics g,Color c,float x,float y,float w,float h){using var b=new SolidBrush(c);g.FillEllipse(b,x,y,w,h);}
    static void P(Graphics g,Color c,params PointF[] p){using var b=new SolidBrush(c);g.FillPolygon(b,p);}
    static void L(Graphics g,Color c,float w,float x,float y,float x2,float y2){using var pen=new Pen(c,w);g.DrawLine(pen,x,y,x2,y2);}
    Rectangle Add(int w,int h,Action<Graphics> paint) {
        if(cursorX+w+2>Atlas.Width){cursorX=2;cursorY+=rowHeight+4;rowHeight=0;}
        if(cursorY+h+2>Atlas.Height)throw new InvalidOperationException("Art atlas full");
        var rect=new Rectangle(cursorX,cursorY,w,h);
        using(var g=Graphics.FromImage(Atlas)){g.SetClip(rect);g.TranslateTransform(cursorX,cursorY);g.SmoothingMode=SmoothingMode.None;paint(g);}
        cursorX+=w+4;rowHeight=Math.Max(rowHeight,h);return rect;
    }
    public FarmArt() {
        for(int i=0;i<fade.Length;i++){fade[i]=new ImageAttributes();fade[i].SetColorMatrix(new ColorMatrix{Matrix33=i/64f});}
        for(int i=0;i<3;i++){int kind=i;WeatherIcons[i]=Add(32,32,g=>{E(g,C("647d7c"),1,8,30,14);E(g,C("647d7c"),8,3,18,19);E(g,C("c3d1be"),2,7,27,12);E(g,C("dde4ca"),8,4,15,13);if(kind==2){for(int j=0;j<3;j++){L(g,C("b8d3d2"),1,5+j*10,23,5+j*10,30);L(g,C("b8d3d2"),1,2+j*10,26,8+j*10,26);}}else for(int j=0;j<3;j++){L(g,C("83b3be"),2,7+j*8,23,5+j*8,28);}if(kind==1)P(g,C("e5c981"),new(19,14),new(13,24),new(17,24),new(14,31),new(24,20),new(19,20));});}
        Shadow=Add(128,32,g=>{g.SmoothingMode=SmoothingMode.AntiAlias;for(int i=0;i<5;i++)E(g,Color.FromArgb(7+i*3,36,51,37),i*5,i*2,128-i*10,32-i*4);});
        Glow=Add(144,112,g=>{g.SmoothingMode=SmoothingMode.AntiAlias;for(int i=0;i<10;i++)E(g,Color.FromArgb(6+i,250,218,149),i*5,i*4,144-i*10,112-i*8);});
        Barrel=Add(30,40,g=>{E(g,C("66513f"),1,1,28,12);R(g,C("a17750"),2,7,26,27);E(g,C("8f6746"),2,27,26,11);for(int i=0;i<5;i++){R(g,C("c49661"),4+i*5,7,2,26);R(g,C("76563f"),7+i*5,7,1,27);}R(g,C("5b6556"),2,12,26,3);R(g,C("9da18b"),3,12,24,1);R(g,C("5b6556"),2,28,26,3);E(g,C("d0ab77"),2,1,26,10);E(g,C("a27f54"),4,3,22,6);R(g,C("e1c493"),7,3,9,1);});
        Mailbox=Add(24,43,g=>{R(g,C("886847"),10,13,4,30);R(g,C("d2ab76"),10,14,1,28);R(g,C("476652"),3,4,19,17);E(g,C("5e7f66"),3,0,19,11);R(g,C("7f9b78"),5,4,15,12);R(g,C("42614d"),5,14,15,2);R(g,C("d4c79b"),7,8,11,3);R(g,C("946a4e"),21,1,2,15);R(g,C("d28b63"),18,1,5,5);});
        for(int season=0;season<4;season++)for(int variation=0;variation<3;variation++){int s=season,v=variation;Trees[s,v]=Add(94,100,g=>Tree(g,s,v));}
        Trunk=Add(94,30,g=>{P(g,Ink,new(34,29),new(35,4),new(49,0),new(53,26),new(60,29));P(g,Wood,new(38,27),new(38,0),new(49,0),new(49,27));R(g,LightWood,39,2,3,23);L(g,C("754c37"),2,46,3,45,26);P(g,Wood,new(41,10),new(26,1),new(33,1),new(46,6));R(g,C("d5a069"),39,27,16,2);});
        for(int i=0;i<2;i++){int n=i;Houses[n]=Add(146,136,g=>House(g,n==1));}
        for(int i=0;i<3;i++){int n=i;Soils[n]=Add(35,35,g=>Soil(g,n));}
        string[] crops=Farm.Crops.Keys.ToArray();
        for(int crop=0;crop<8;crop++)for(int stage=0;stage<4;stage++){int c=crop,s=stage;Crops[c,s]=Add(36,45,g=>Crop(g,crops[c],s));}
        for(int color=0;color<4;color++)for(int facing=0;facing<4;facing++)for(int frame=0;frame<8;frame++){int c=color,f=facing,n=frame;Farmers[c,f,n]=Add(36,60,g=>Farmer(g,c,f,n));}
        for(int i=0;i<Icons.Length;i++){int n=i;Icons[n]=Add(32,32,g=>Icon(g,n));}
        Crate=Add(50,49,g=>{P(g,C("5e4937"),new(2,44),new(41,44),new(49,33),new(49,9),new(9,2),new(1,11));R(g,Wood,2,12,37,30);P(g,C("754932"),new(39,12),new(48,5),new(48,34),new(39,43));P(g,LightWood,new(2,12),new(10,3),new(48,5),new(39,12));for(int i=0;i<3;i++){R(g,C("dbab70"),4,16+i*9,33,2);R(g,C("805039"),4,18+i*9,33,1);}R(g,C("e4c68d"),8,22,23,12);P(g,C("7e8754"),new(15,28),new(20,25),new(25,28),new(20,31));for(int i=0;i<4;i++)R(g,C("4c4437"),5+i%2*29,16+i/2*20,2,2);});
        Rock=Add(24,16,g=>{P(g,C("716c5b"),new(0,11),new(6,2),new(17,0),new(24,8),new(21,14),new(4,15));P(g,C("b5b39b"),new(1,9),new(6,2),new(17,1),new(22,7),new(19,10),new(8,12));L(g,C("d6d3b8"),2,7,3,16,3);L(g,C("8e8c77"),2,15,7,19,10);});
        Bush=Add(63,38,g=>{var rng=new Random(58);for(int i=0;i<22;i++){int x=rng.Next(2,48),y=rng.Next(3,19);E(g,C("3c5b3c"),x,y,17,19);E(g,C("799748"),x+1,y-1,14,15);R(g,C("a5b76a"),x+3,y,6,2);}for(int i=0;i<8;i++){int x=8+i*17%45,y=7+i*13%22;R(g,C("bd5970"),x,y,4,4);R(g,C("f7b491"),x,y,2,2);}});
        Reeds=Add(24,40,g=>{for(int i=0;i<5;i++){L(g,C("576743"),2,4+i*4,38,3+i*4,10+i%3*5);L(g,C("98a666"),1,5+i*4,37,12+i*2,22);R(g,C("886c49"),1+i*4,7+i%3*5,4,8);R(g,C("b89562"),1+i*4,7+i%3*5,2,6);}});
        for(int season=0;season<4;season++){Ground[season]=MakeGround(season);grassTiles[season]=new Bitmap(128,128,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(grassTiles[season]))g.DrawImage(Ground[season],new Rectangle(0,0,128,128),new Rectangle(400,0,128,128),GraphicsUnit.Pixel);OuterGrass[season]=new TextureBrush(grassTiles[season],WrapMode.Tile);}
    }
    static void Tree(Graphics g,int season,int variation) {
        Color[] baseColors={C("779c52"),C("6a9450"),C("cf8656"),C("b9caca")};
        Color mid=baseColors[season],dark=season==3?C("768f8d"):Shade(mid,-42),light=season==3?C("e0e9de"):Shade(mid,32);
        var rng=new Random(917+variation*71);
        // Overlapping leaf clusters form an irregular silhouette and a shadowed underside.
        E(g,Ink,9,30,74,55);E(g,Ink,18,12,56,62);E(g,Ink,30,3,34,51);
        E(g,dark,11,31,70,52);E(g,dark,20,14,52,61);E(g,dark,31,5,30,49);
        for(int i=0;i<42;i++){int x=rng.Next(17,69),y=rng.Next(16,69);if((x-44)*(x-44)+(y-43)*(y-43)>1100)continue;int w=rng.Next(13,23);E(g,Shade(dark,rng.Next(5,26)),x-5,y+3,w,w*.7f);E(g,Shade(mid,rng.Next(-9,10)),x-7,y,w,w*.7f);}
        for(int i=0;i<30;i++){int x=rng.Next(18,64),y=rng.Next(12,58);int width=rng.Next(5,11);R(g,light,x,y,width,2);R(g,Shade(mid,19),x-2,y+2,width+4,3);R(g,Shade(mid,-14),x+1,y+6,width,2);}
        if(season==0)for(int i=0;i<11;i++){int x=18+i*17%53,y=21+i*19%41;R(g,C("f1d4ae"),x,y,4,4);R(g,C("edb8a4"),x-2,y+1,8,2);R(g,C("d58b77"),x+1,y+1,2,2);}
        if(season==1||season==2)for(int i=0;i<7;i++){int x=22+i*17%47,y=32+i*21%36;E(g,C("8f4c3e"),x,y,7,7);E(g,season==1?C("dabc67"):C("d75e4e"),x,y,6,5);R(g,C("f6d890"),x+1,y+1,2,1);}
        if(season==3){for(int i=0;i<18;i++){int x=20+i*13%51,y=16+i*17%38;R(g,C("f3f4e8"),x,y,10,3);R(g,C("d4e2de"),x+2,y+3,8,2);}}
    }
    static void House(Graphics g,bool shop) {
        Color roof=shop?C("657c73"):C("b76c50"),roofLight=shop?C("99af96"):C("dd9970");
        P(g,Ink,new(10,57),new(69,4),new(135,57),new(127,64),new(127,118),new(13,118),new(13,63));
        R(g,C("aa8666"),17,51,109,64);R(g,C("dab58a"),18,54,94,59);R(g,C("8c684f"),113,58,12,56);
        for(int x=20;x<112;x+=10){R(g,C("bd936c"),x,55,1,57);R(g,C("e5c99d"),x+1,55,2,56);R(g,C("c89e76"),x+6,58,1,53);}
        for(int i=0;i<25;i++){int x=22+i*23%82,y=59+i*17%51;R(g,C("aa805e"),x,y,2,4);}
        R(g,C("70594c"),14,112,114,7);R(g,C("b9a58b"),17,116,108,7);for(int x=18;x<124;x+=16){R(g,C("dfcab0"),x,118,14,3);R(g,C("8e8271"),x+13,117,1,5);}
        P(g,C("754b41"),new(5,59),new(67,2),new(140,58),new(131,68),new(66,20),new(14,67));
        P(g,roof,new(7,57),new(66,5),new(135,56),new(121,59),new(66,15),new(17,59));
        for(int row=0;row<7;row++){int y=13+row*6,start=64-row*7,end=69+row*8;R(g,Shade(roof,-24),start,y+3,end-start,2);R(g,roofLight,start,y,end-start,1);for(int x=start+(row%2)*7;x<end;x+=13)R(g,Shade(roof,-31),x,y,1,4);}
        L(g,C("f2b88a"),2,66,7,15,55);L(g,Shade(roof,-38),3,67,8,130,59);R(g,C("f0d3a6"),16,61,6,52);R(g,C("957555"),21,62,2,51);R(g,C("efcea0"),106,62,6,50);
        R(g,Ink,100,10,14,22);R(g,C("9b8d79"),102,10,10,21);R(g,C("d0bea2"),101,9,13,4);for(int i=0;i<3;i++)R(g,C("726f64"),104,15+i*5,6,1);
        // A painted sign and awning distinguish the market from the cottage.
        if(shop){R(g,C("3d604d"),28,56,70,14);for(int i=0;i<7;i++)R(g,i%2==0?C("f0d9ad"):C("7d9565"),28+i*10,57,10,9);for(int i=0;i<7;i++)P(g,i%2==0?Cream:C("7d9565"),new(28+i*10,64),new(38+i*10,64),new(36+i*10,70),new(30+i*10,70));R(g,C("74513a"),37,71,27,12);R(g,LightWood,39,73,23,8);R(g,C("77945c"),47,74,6,6);}
        int wy=shop?84:73;R(g,C("6e5140"),29,wy-2,30,24);R(g,C("eacb97"),31,wy,26,20);R(g,C("8eacb0"),33,wy+2,22,16);P(g,C("cce0cc"),new(34,wy+3),new(46,wy+3),new(34,wy+15));R(g,C("f5dfb4"),43,wy+1,3,18);R(g,C("f5dfb4"),32,wy+9,23,2);R(g,C("7c9865"),24,wy,5,22);R(g,C("58734e"),59,wy,5,22);for(int i=0;i<4;i++)R(g,C("416347"),24,wy+4+i*5,5,1);
        R(g,Ink,73,77,27,39);R(g,C("edcfa6"),75,78,23,36);R(g,C("75533e"),78,81,17,33);R(g,C("a3754c"),80,83,13,27);R(g,C("c8935d"),82,84,2,24);R(g,C("694d3b"),79,103,14,2);R(g,C("e8c172"),90,98,3,3);
        R(g,C("705947"),67,122,36,3);R(g,C("c9b18c"),65,125,40,4);R(g,C("e8d1a9"),63,129,44,3);
        if(!shop){R(g,C("8f6048"),25,99,39,6);R(g,C("d49a64"),25,100,39,2);for(int i=0;i<7;i++){R(g,C("4d6f43"),28+i*5,95,5,6);R(g,i%2==0?C("e5a29b"):Cream,29+i*5,93,4,4);R(g,C("d28c64"),30+i*5,94,1,1);}}
    }
    static void Soil(Graphics g,int wet) {
        Color top=wet==2?C("795743"):wet==1?C("97704e"):C("b58c60");
        R(g,C("624735"),1,4,33,29);R(g,C("805938"),1,1,33,29);R(g,top,2,1,31,28);R(g,Shade(top,32),3,1,28,1);
        for(int i=0;i<3;i++){R(g,Shade(top,-27),4,7+i*8,27,2);R(g,Shade(top,19),5,10+i*8,25,1);}
        var rng=new Random(56);for(int i=0;i<27;i++)R(g,Shade(top,rng.Next(-30,31)),rng.Next(3,31),rng.Next(3,28),rng.Next(1,3),1);
        R(g,C("9a714a"),1,30,33,2);R(g,C("604635"),3,32,29,1);
        if(wet==2){R(g,C("838b79"),6,21,15,2);R(g,C("aaa990"),8,21,8,1);}
    }
    static void Crop(Graphics g,string crop,int stage) {
        g.TranslateTransform(18,40);Color leaf=C("57814c"),light=C("a5bc70"),dark=C("3c6140");
        if(stage==0){R(g,dark,-1,-7,2,8);P(g,leaf,new(-1,-4),new(-8,-8),new(-7,-3),new(0,-1));P(g,light,new(0,-5),new(6,-10),new(9,-8),new(3,-3));return;}
        int height=stage==1?17:27;R(g,dark,-1,-height,3,height);
        for(int i=0;i<(stage==1?2:4);i++){int y=-7-i*5;P(g,dark,new(0,y),new(-12,y-6),new(-11,y-1),new(-3,y+2));P(g,leaf,new(0,y),new(-12,y-7),new(-8,y-3));P(g,leaf,new(1,y-3),new(13,y-8),new(11,y-1),new(3,y+1));L(g,light,1,2,y-3,10,y-6);}
        if(stage<3){R(g,light,-3,-height-1,6,2);return;}
        switch(crop){
            case "carrot":P(g,C("b86e39"),new(-10,-14),new(8,-14),new(2,4),new(-3,6));P(g,C("edaa50"),new(-8,-13),new(5,-13),new(-2,5));R(g,C("f8d178"),-6,-11,3,8);R(g,C("cf843f"),-4,-5,8,2);break;
            case "turnip":E(g,C("d4c9a6"),-10,-16,20,20);E(g,C("f3ecd1"),-9,-16,17,16);E(g,C("a3789b"),-9,-17,17,8);R(g,C("dcc0c7"),-6,-16,7,2);L(g,C("f3ecd1"),2,0,0,2,7);break;
            case "potato":E(g,C("9b734a"),-12,-12,25,16);E(g,C("cfac73"),-11,-13,22,13);R(g,C("ecd298"),-7,-11,7,2);R(g,C("9e8154"),5,-6,2,2);R(g,C("9e8154"),-6,-3,2,1);break;
            case "tomato":for(int i=0;i<3;i++){int x=-12+i*8,y=-12-i%2*7;E(g,C("914d3d"),x,y,12,12);E(g,C("df7651"),x,y,10,10);R(g,C("f6b489"),x+2,y+2,4,2);R(g,dark,x+4,y-1,3,3);}break;
            case "corn":E(g,C("a38843"),-5,-31,12,30);E(g,C("e3be60"),-5,-31,10,28);for(int y=-28;y<-3;y+=5){R(g,C("f8e09b"),-3,y,3,3);R(g,C("d3a148"),2,y,2,3);}P(g,leaf,new(-3,0),new(-13,-23),new(-9,-4),new(0,2));P(g,light,new(3,0),new(14,-20),new(10,-2),new(0,2));break;
            case "berry":for(int i=0;i<3;i++){int x=-12+i*8,y=-11-i%2*8;P(g,C("a14f50"),new(x,y),new(x+12,y),new(x+10,y+9),new(x+5,y+14),new(x,y+6));P(g,C("d67b76"),new(x+1,y+1),new(x+10,y+1),new(x+5,y+12));for(int j=0;j<3;j++)R(g,C("f6dba6"),x+3+j%2*3,y+3+j*3,1,2);R(g,leaf,x+3,y-2,5,4);}break;
            case "pumpkin":E(g,C("8c603e"),-15,-18,30,23);E(g,C("c28347"),-14,-19,28,22);E(g,C("e5a55c"),-9,-19,18,22);E(g,C("f1bd76"),-4,-19,8,22);L(g,C("9e6943"),1,-8,-14,-8,0);L(g,C("b37c46"),1,7,-15,7,1);R(g,dark,-2,-24,4,6);break;
            default:E(g,dark,-15,-20,30,24);E(g,leaf,-12,-22,26,22);E(g,light,-8,-21,18,18);E(g,C("d0d69a"),-5,-19,13,12);L(g,C("a5bb79"),1,0,-17,2,-3);L(g,C("dce2ac"),1,-6,-13,1,-7);L(g,C("73965b"),1,-10,-13,-4,-4);break;
        }
    }
    static void Farmer(Graphics g,int tint,int facing,int frame) {
        int step=(int)Math.Round(Math.Sin(frame*Math.PI/4)*2),bob=frame%4==1?1:0;
        Color[] shirts={C("b86554"),C("678f9d"),C("94739f"),C("c4a362")};Color shirt=shirts[tint],skin=C("e4b48b");
        g.TranslateTransform(18,55+bob);R(g,C("494b44"),-7, -13,6,17+step);R(g,C("494b44"),2,-13,6,17-step);R(g,C("806348"),-8,2+step,8,4);R(g,C("b28a61"),-7,2+step,4,1);R(g,C("806348"),1,2-step,8,4);R(g,C("b28a61"),2,2-step,4,1);
        R(g,Shade(shirt,-30),-10,-32,20,22);R(g,shirt,-8,-32,16,17);R(g,Shade(shirt,21),-8,-31,4,13);
        R(g,skin,-13,-28,4,16+step);R(g,skin,9,-28,4,16-step);R(g,C("b58268"),-13,-15+step,4,4);R(g,C("b58268"),9,-15-step,4,4);
        R(g,C("677e78"),-6,-28,3,14);R(g,C("b7c0a0"),-6,-28,1,13);R(g,C("677e78"),4,-28,3,14);R(g,C("809185"),-6,-18,13,10);R(g,C("a9b6a0"),-5,-17,11,2);R(g,C("4b655f"),-5,-14,10,1);
        R(g,C("604d3a"),-9,-46,18,18);R(g,skin,-7,-44,15,15);R(g,C("f3caa0"),-6,-43,9,10);R(g,C("b68061"),-4,-29,9,2);
        if(facing==3){R(g,C("765339"),-8,-44,17,12);R(g,C("a16e42"),-6,-43,12,9);R(g,C("e4b48b"),-8,-35,2,5);}
        else {int fx=facing==1?-3:facing==2?3:0;R(g,Ink,-4+fx,-38,2,3);if(facing==0)R(g,Ink,4,-38,2,3);R(g,C("c98b75"),-4+fx,-33,3,2);R(g,C("a06d5e"),fx,-31,3,1);}
        R(g,C("785d3b"),-15,-46,31,5);R(g,C("d2ac68"),-16,-48,33,4);R(g,C("f0d69b"),-13,-49,27,2);R(g,C("a7814d"),-10,-57,21,9);R(g,C("ddbb78"),-8,-57,17,8);R(g,C("f1d89e"),-6,-57,10,2);R(g,C("98734b"),-9,-51,19,2);R(g,C("d8cead"),6,-52,3,2);
    }
    static void Icon(Graphics g,int kind) {
        g.TranslateTransform(16,17);
        switch(kind){
            case 0:R(g,C("ab7750"),-9,-10,18,23);R(g,C("dfbf85"),-8,-9,16,21);R(g,C("fbdfa0"),-6,-9,12,3);P(g,C("638855"),new(0,5),new(-6,-3),new(0,0),new(6,-5),new(4,4));R(g,C("8d6a41"),-5,8,10,2);break;
            case 1:R(g,C("4c6772"),-10,-5,18,18);R(g,C("79abb0"),-9,-5,16,14);R(g,C("b8d4cc"),-8,-3,5,11);P(g,C("6a919b"),new(7,-3),new(14,-9),new(15,-4),new(8,5));L(g,C("667d85"),3,-6,-4,-6,-10);L(g,C("667d85"),3,-6,-10,6,-10);R(g,C("d3ece0"),10,-10,6,2);break;
            case 2:L(g,C("936945"),4,-9,13,7,-8);P(g,C("4f6770"),new(4,-10),new(8,-15),new(15,-12),new(15,-4),new(11,1),new(8,-1),new(11,-7));L(g,C("d6ded0"),2,8,-13,14,-10);break;
            case 3:R(g,C("526c4b"),-8,-7,16,21);R(g,C("91b68b"),-7,-6,14,18);R(g,C("c8d0a5"),-5,-3,10,11);R(g,C("6c8a56"),-5,-3,10,2);R(g,C("595d46"),-4,-13,8,6);R(g,C("a9b788"),-2,-15,9,3);break;
            case 4:P(g,C("7c7654"),new(-10,13),new(-8,-8),new(7,-8),new(11,13));P(g,C("b8b784"),new(-8,12),new(-6,-7),new(5,-7),new(9,12));R(g,C("74945b"),-5,-2,11,8);R(g,C("d1d39b"),-3,0,7,4);R(g,C("6b6850"),-6,-11,11,4);break;
            case 5:L(g,C("7e583c"),5,-8,14,4,-13);L(g,C("d4a474"),2,-9,13,3,-13);L(g,C("7e583c"),4,5,13,11,-8);L(g,C("d4a474"),1,4,13,10,-8);break;
            case 6:L(g,C("926b45"),4,-8,13,7,-13);R(g,C("58685e"),-9,-14,22,6);R(g,C("b9c5af"),-8,-14,20,2);break;
            case 7:E(g,C("9b763d"),-13,-13,26,26);E(g,C("e1b76b"),-12,-13,24,23);E(g,C("f4d596"),-9,-10,18,17);R(g,C("ac864e"),-2,-7,4,13);R(g,C("ac864e"),-6,-2,12,3);break;
            case 8:P(g,C("b28845"),new(1,-15),new(-11,3),new(-1,3),new(-4,15),new(12,-5),new(2,-5));P(g,C("e8ce7d"),new(0,-12),new(-8,1),new(1,1),new(-1,10),new(8,-3),new(0,-3));break;
            case 9:E(g,C("947c5a"),-12,-12,24,24);E(g,C("e5d5ab"),-10,-10,20,20);L(g,Ink,2,0,0,0,-7);L(g,Ink,2,0,0,6,3);R(g,C("c09d69"),-4,-15,8,3);break;
            case 10:E(g,C("a68c4d"),-10,-12,20,20);E(g,C("f1d488"),-9,-12,18,18);for(int i=0;i<8;i++){double a=i*Math.PI/4;L(g,C("d8b66e"),2,(float)Math.Cos(a)*12,(float)Math.Sin(a)*12-3,(float)Math.Cos(a)*15,(float)Math.Sin(a)*15-3);}break;
            default:R(g,C("705a43"),-12,-7,24,19);R(g,C("bd9364"),-10,-6,20,17);R(g,C("dfbf86"),-8,-3,16,4);L(g,C("705a43"),3,-7,-6,-7,-13);L(g,C("705a43"),3,-7,-13,7,-13);L(g,C("705a43"),3,7,-13,7,-6);R(g,C("8e704c"),-8,6,16,2);break;
        }
    }
    Bitmap MakeGround(int season) {
        var image=new Bitmap(960,800,PixelFormat.Format32bppPArgb);using var g=Graphics.FromImage(image);
        Color grass=season switch {1=>C("8fa363"),2=>C("bea16b"),3=>C("c8d5cd"),_=>C("91ac70")};
        g.Clear(grass);var rng=new Random(1819);g.SmoothingMode=SmoothingMode.None;
        for(int i=0;i<1100;i++){int x=rng.Next(960),y=rng.Next(800),w=rng.Next(15,64);E(g,Color.FromArgb(18,Shade(grass,i%2==0?25:-28)),x,y,w,rng.Next(9,30));}
        for(int i=0;i<11000;i++){int x=rng.Next(960),y=rng.Next(800);R(g,Shade(grass,rng.Next(-18,19)),x,y,rng.Next(1,4),1);}
        for(int i=0;i<1050;i++){int x=rng.Next(960),y=rng.Next(800);Color dark=Shade(grass,-25),light=Shade(grass,19);L(g,dark,1,x,y,x-2,y-4);L(g,dark,1,x+2,y,x+3,y-5);R(g,light,x-1,y-4,1,2);}
        // Paths with imperfect stone edges, worn centres and grass breaking through joints.
        Path(g,rng,new(187,258,657,32),season);Path(g,rng,new(230,261,37,384),season);
        for(int x=33;x<903;x+=34){R(g,C("786446"),x,136,6,27);R(g,C("b79569"),x-1,132,6,29);R(g,C("dcc493"),x-1,132,2,26);R(g,C("f3dab0"),x-1,132,6,2);R(g,C("594f39"),x+1,149,2,2);}
        R(g,C("96784e"),33,140,873,5);R(g,C("c4a577"),33,139,873,3);R(g,C("e2c597"),33,139,873,1);R(g,C("92734f"),33,153,873,4);R(g,C("c4a577"),33,152,873,2);
        // Flower beds, border rocks, scattered sticks and low garden fences are decorative.
        for(int i=0;i<125;i++){int x=15+i*173%932,y=157+i*139%598;if(x>270&&x<540&&y>295&&y<477)continue;if(x>615&&x<850&&y>510&&y<681)continue;Color leaf=Shade(grass,-34);L(g,leaf,1,x,y,x,y-5);if(season==3){R(g,C("edf1e4"),x-1,y-5,6,2);continue;}Color flower=i%3==0?C("e8cba0"):i%3==1?C("e4b4a2"):C("d9d5b8");R(g,flower,x-2,y-7,5,3);R(g,flower,x-1,y-8,3,5);R(g,C("c4a56a"),x,y-6,1,1);R(g,leaf,x+1,y-2,4,1);}
        for(int i=0;i<35;i++){int x=45+i*97%864,y=191+i*151%526;if(x>280&&x<552&&y>301&&y<496)continue;if(x>620&&y>510)continue;R(g,C("a7a486"),x,y,6,3);R(g,C("d1c6a1"),x+1,y,4,1);}
        // Curated flower borders and garden details break up the open lawn.
        foreach(var bed in new[]{new Rectangle(66,224,51,24),new Rectangle(252,193,62,36),new Rectangle(582,204,72,34),new Rectangle(799,321,61,33),new Rectangle(530,463,65,35)}) {
            E(g,Shade(grass,-20),bed.X,bed.Y,bed.Width,bed.Height);
            for(int i=0;i<16;i++){int x=bed.X+4+i*13%(bed.Width-8),y=bed.Y+5+i*17%(bed.Height-8);R(g,season==3?C("e6ece2"):C("5d7b49"),x,y,4,5);if(season==3)continue;Color flower=i%3==0?C("d99a90"):i%3==1?C("e9d3a2"):C("b6afc2");R(g,flower,x-1,y-2,6,3);R(g,flower,x+1,y-3,2,5);R(g,C("c3a965"),x+1,y-1,1,1);}
        }
        for(int i=0;i<9;i++){float x=575+i%3*27,y=374+i/3*27;g.DrawImage(Atlas,new RectangleF(x,y,24,16),Rock,GraphicsUnit.Pixel);}
        g.DrawImage(Atlas,new RectangleF(81,259,26,35),Barrel,GraphicsUnit.Pixel);g.DrawImage(Atlas,new RectangleF(246,245,24,43),Mailbox,GraphicsUnit.Pixel);
        g.DrawImage(Atlas,new RectangleF(834,254,26,35),Barrel,GraphicsUnit.Pixel);
        // A small picnic bench provides another recognisable, non-interactive garden prop.
        R(g,C("6a593e"),602,301,5,31);R(g,C("6a593e"),666,301,5,31);R(g,C("a37f52"),595,299,85,11);R(g,C("dec396"),595,299,85,2);R(g,C("745c3f"),596,314,83,4);R(g,C("b69260"),593,313,87,4);R(g,C("ddbf8c"),593,313,87,1);
        for(int i=0;i<4;i++){int x=320+i*48;R(g,C("ab8155"),x,485,5,22);R(g,C("d9b384"),x,484,2,22);R(g,C("b18b60"),x,490,47,4);R(g,C("dfbd8d"),x,490,47,1);}
        // Cached pond: layered shallow water, stone banks and a timber footbridge.
        E(g,C("60765a"),616,524,230,150);E(g,C("aca47f"),618,520,228,150);E(g,C("d8c8a2"),621,518,221,144);
        E(g,season==3?C("91b8bc"):C("62959b"),628,525,210,131);E(g,season==3?C("b3d0d0"):C("82b5b0"),635,529,194,115);E(g,season==3?C("c1d7d4"):C("a2c7bd"),644,532,172,54);
        for(int i=0;i<95;i++){int x=640+rng.Next(180),y=534+rng.Next(104);if((x-733)*(x-733)/10000d+(y-590)*(y-590)/3600d>1)continue;R(g,Color.FromArgb(38,230,240,203),x,y,rng.Next(4,15),1);}
        for(int i=0;i<20;i++){double a=i*Math.PI/10;float x=733+(float)Math.Cos(a)*107,y=588+(float)Math.Sin(a)*65;E(g,C("7d826a"),x-6,y-3,15,9);E(g,C("babb98"),x-6,y-5,13,8);R(g,C("dfe0b8"),x-3,y-4,5,1);}
        R(g,C("705b42"),759,548,73,28);for(int x=760;x<832;x+=9){R(g,C("b18c5e"),x,548,8,25);R(g,C("d8b581"),x,549,7,2);R(g,C("8c6744"),x,567,7,1);}R(g,C("9e7a50"),764,544,4,33);R(g,C("ddbf8b"),764,544,1,33);
        return image;
    }
    static void Path(Graphics g,Random rng,Rectangle r,int season) {
        R(g,C("7c805b"),r.X-2,r.Y+3,r.Width+4,r.Height+2);R(g,C("c6b58e"),r.X,r.Y,r.Width,r.Height);R(g,C("dfcba3"),r.X+2,r.Y+2,r.Width-4,r.Height-4);
        for(int y=r.Y+2;y<r.Bottom;y+=15)for(int x=r.X+2;x<r.Right;x+=24){int w=Math.Min(22,r.Right-x),h=Math.Min(13,r.Bottom-y);R(g,C("baa884"),x,y,w,h);R(g,C("d9c7a0"),x,y,w,h-2);R(g,C("eedcb7"),x+1,y+1,Math.Max(1,w-2),1);R(g,C("b9ac89"),x+1,y+h-3,Math.Max(1,w-2),1);if(rng.Next(7)==0)R(g,C("9b9d6c"),x,y,3,2);}
    }
    public void Draw(Graphics g,Rectangle sprite,float x,float y,float width=0,float height=0) {
        if(width==0)width=sprite.Width;if(height==0)height=sprite.Height;
        g.DrawImage(Atlas,new RectangleF(x,y,width,height),sprite,GraphicsUnit.Pixel);
    }
    public int CropIndex(string key)=>key switch {"carrot"=>0,"turnip"=>1,"potato"=>2,"tomato"=>3,"corn"=>4,"berry"=>5,"pumpkin"=>6,"cabbage"=>7,_=>0};
    public void DrawFade(Graphics g,Rectangle sprite,Rectangle target,float opacity)=>g.DrawImage(Atlas,target,sprite.X,sprite.Y,sprite.Width,sprite.Height,GraphicsUnit.Pixel,fade[Math.Clamp((int)(opacity*64),0,64)]);
    public void DrawLayerFade(Graphics g,Image image,Rectangle target,float opacity)=>g.DrawImage(image,target,0,0,image.Width,image.Height,GraphicsUnit.Pixel,fade[Math.Clamp((int)(opacity*64),0,64)]);
    public void Export(string path){Atlas.Save(path,ImageFormat.Png);}
    public void Dispose(){Atlas.Dispose();foreach(var brush in OuterGrass)brush?.Dispose();foreach(var tile in grassTiles)tile?.Dispose();foreach(var ground in Ground)ground?.Dispose();foreach(var item in fade)item?.Dispose();}
}
