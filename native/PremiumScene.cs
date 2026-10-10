using System.Drawing.Drawing2D;
using System.Text.Json;

namespace SuanPleng;
public sealed partial class GameForm {
    readonly FarmArt art=new();
    readonly Dictionary<int,SolidBrush> brushCache=new();
    readonly Stack<Spark> sparkPool=new(384);
    readonly SceneObject[] sceneObjects=new SceneObject[64];
    struct SceneObject { public float Depth;public int Kind,Index;public Farmer? Player; }
    readonly record struct VisualPreferences(int Quality,bool ReducedMotion,bool Sound,float Zoom);
    int quality=2;
    float cameraX=18,cameraY=90,zoom=1.35f;
    bool cameraReady;
    Bitmap? previousDayLayer;
    double dayTransitionStart=-10;
    double displayMinute=360,weatherBlend;
    int displaySeason=-1;
    string previousWeather="",preferencesPath="";
    const float WorldX=32,WorldY=112,TileSize=35;
    static readonly RectangleF SceneView=new(16,87,1248,601);
    string QualityName=>quality switch {0=>"ต่ำ",1=>"กลาง",_=>"สูง"};
    void LoadVisualPreferences(string data) {
        preferencesPath=Path.Combine(data,"visual-settings.json");
        try {if(File.Exists(preferencesPath)){var p=JsonSerializer.Deserialize<VisualPreferences>(File.ReadAllText(preferencesPath));quality=Math.Clamp(p.Quality,0,2);reducedMotion=p.ReducedMotion;soundEnabled=p.Sound;zoom=Math.Clamp(p.Zoom,1,1.7f);}}
        catch(Exception e)when(e is IOException or JsonException or UnauthorizedAccessException){ /* Graphics preferences never invalidate farm saves. */ }
        for(int i=0;i<160;i++)sparkPool.Push(new Spark());
    }
    void SaveVisualPreferences() {
        if(renderPreview||preferencesPath=="")return;
        try {File.WriteAllText(preferencesPath,JsonSerializer.Serialize(new VisualPreferences(quality,reducedMotion,soundEnabled,zoom)));}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){Notify("บันทึกการตั้งค่าภาพไม่ได้");}
    }
    void UpdatePresentation(double dt) {
        if(world==null){cameraReady=false;return;}
        float targetX=Math.Clamp(WorldX+(float)px*TileSize-SceneView.Width/zoom/2,0,Math.Max(0,960-SceneView.Width/zoom));
        float targetY=Math.Clamp(WorldY+(float)py*TileSize-SceneView.Height/zoom/2,65,Math.Max(65,790-SceneView.Height/zoom));
        if(!cameraReady||renderPreview){cameraX=targetX;cameraY=targetY;cameraReady=true;displayMinute=world.Minute;}
        else {float blend=(float)(1-Math.Exp(-8*dt));cameraX+=(targetX-cameraX)*blend;cameraY+=(targetY-cameraY)*blend;displayMinute+=(world.Minute-displayMinute)*(1-Math.Exp(-4*dt));}
        double target=world.Weather is "ฝนตก" or "พายุ"?1:0;
        if(renderPreview||displaySeason==-1)weatherBlend=target;else weatherBlend+=(target-weatherBlend)*(1-Math.Exp(-2*dt));
        displaySeason=world.Season;previousWeather=world.Weather;
    }
    PointF SceneToWorld(PointF p)=>new(cameraX+(p.X-SceneView.X)/zoom,cameraY+(p.Y-SceneView.Y)/zoom);
    PointF WorldToScene(PointF p)=>new(SceneView.X+(p.X-cameraX)*zoom,SceneView.Y+(p.Y-cameraY)*zoom);
    void CapturePreviousDay() {
        if(world==null||reducedMotion||quality==0)return;
        previousDayLayer??=new Bitmap((int)SceneView.Width,(int)SceneView.Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var graphics=Graphics.FromImage(previousDayLayer);graphics.Clear(Color.Transparent);graphics.TranslateTransform(-SceneView.X,-SceneView.Y);
        // Do not recursively composite the snapshot being overwritten.
        dayTransitionStart=-10;DrawScene(graphics,world);dayTransitionStart=VisualTime;
    }
    void DrawScene(Graphics g,Farm f) {
        var state=g.Save();g.SetClip(SceneView);g.TranslateTransform(SceneView.X-cameraX*zoom,SceneView.Y-cameraY*zoom);g.ScaleTransform(zoom,zoom);
        g.SmoothingMode=SmoothingMode.None;g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
        float rightEdge=cameraX+SceneView.Width/zoom;if(rightEdge>960)g.FillRectangle(art.OuterGrass[f.Season],960,cameraY,rightEdge-960,SceneView.Height/zoom);
        g.DrawImageUnscaled(art.Ground[f.Season],0,0);
        for(int i=0;i<24;i++)DrawSoil(g,f.Plots[i],i);
        int count=0;
        void Add(int kind,int index,float depth,Farmer? player=null){sceneObjects[count++]=new(){Kind=kind,Index=index,Depth=depth,Player=player};}
        Add(0,0,276);Add(0,1,262);Add(1,0,438);
        for(int i=0;i<9;i++)Add(2,i,i<6?675:400);
        for(int i=0;i<24;i++)Add(3,i,TilePoint(i).Y+31);
        long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();int n=0;
        foreach(var p in f.Players)if(now-p.LastSeen<30000){var a=Avatar(p.Id);if(!a.Initialized){a.X=p.Id==sessionId?px:p.X;a.Y=p.Id==sessionId?py:p.Y;a.Initialized=true;}Add(4,n++,WorldY+(float)a.Y*TileSize+10,p);}
        // Reuse storage and insertion sort: about 40 sprites, no closures or LINQ per frame.
        for(int i=1;i<count;i++){var value=sceneObjects[i];int j=i-1;while(j>=0&&sceneObjects[j].Depth>value.Depth){sceneObjects[j+1]=sceneObjects[j];j--;}sceneObjects[j+1]=value;}
        for(int i=0;i<count;i++){var item=sceneObjects[i];switch(item.Kind){
            case 0:RichHouse(g,item.Index==0?102:697,147,item.Index==0?1: .9f,item.Index==1);break;
            case 1:DrawCrate(g,725,390);break;
            case 2:{float x=item.Index<6?40+(item.Index<3?item.Index*70:(item.Index+3)*70):44+(item.Index-6)*75;RichTree(g,x,item.Index<6?552:290,item.Index<6?1: .76f);break;}
            case 3:DrawPlant(g,f.Plots[item.Index],item.Index);break;
            case 4:{var a=Avatar(item.Player!.Id);DrawFarmerSprite(g,WorldX+(float)a.X*TileSize,WorldY+(float)a.Y*TileSize,a,item.Index,item.Player.Sleeping);break;}
        }}
        for(int i=0;i<count;i++)sceneObjects[i].Player=null;
        DrawPond(g,f);
        if(f.Greenhouse){Round(g,Color.FromArgb(30,204,230,209),new(309,312,211,145),3);Line(g,Color.FromArgb(164,215,226,207),2,311,312,519,312);for(int i=0;i<4;i++)Line(g,Color.FromArgb(126,215,226,207),2,311+i*68,312,311+i*68,457);}
        DrawWind(g,f);DrawLighting(g,f);DrawAtmosphere(g,f);DrawSparks(g);
        foreach(var p in f.Players)if(now-p.LastSeen<30000){var a=Avatar(p.Id);float x=WorldX+(float)a.X*TileSize,y=WorldY+(float)a.Y*TileSize;if(p.Id!=sessionId||f.Players.Count>1){float width=Math.Min(145,Math.Max(48,g.MeasureString(p.Name,F(10,true)).Width+12));Round(g,Color.FromArgb(216,249,237,203),new(x-width/2,y-77,width,18),3);TextAt(g,p.Name,x-width/2+6,y-76,10,Moss,true);}}
        g.Restore(state);
        if(previousDayLayer!=null&&VisualTime-dayTransitionStart<.85&&!reducedMotion&&quality>0)art.DrawLayerFade(g,previousDayLayer,Rectangle.Round(SceneView),(float)(1-(VisualTime-dayTransitionStart)/.85));
        // A thin wood border anchors the world without sacrificing playable area.
        Line(g,Color.FromArgb(137,113,78),1,SceneView.X,SceneView.Bottom,SceneView.Right,SceneView.Bottom);
        DrawInteractionHint(g,f);
    }
    void SoftShadow(Graphics g,float x,float y,float w,float h) {
        if(quality==0)Oval(g,Color.FromArgb(34,40,52,37),x,y,w,h);
        else art.Draw(g,art.Shadow,x-4,y-3,w+8,h+6);
    }
    void DrawSoil(Graphics g,Plot p,int index){var t=TilePoint(index);art.Draw(g,art.Soils[p.Moisture>80?2:p.Moisture>=25?1:0],t.X,t.Y);}
    void DrawPlant(Graphics g,Plot p,int index) {
        var t=TilePoint(index);if(p.Stake){Box(g,Color.FromArgb(102,77,48),t.X+29,t.Y-6,3,38);Box(g,Color.FromArgb(205,177,113),t.X+29,t.Y-6,1,37);}
        if(p.Crop=="")return;
        float progress=(float)Math.Clamp(p.Growth/Farm.Crops[p.Crop].Days,0,1);int stage=p.RipeDay>=0?3:progress>.65?2:progress>.25?1:0;
        float sway=reducedMotion||quality==0?0:(float)Math.Sin(VisualTime*(world?.Weather=="พายุ"?3:1.3)+index*.7)*.6f;
        SoftShadow(g,t.X+5,t.Y+23,25,8);art.Draw(g,art.Crops[art.CropIndex(p.Crop),stage],t.X+sway,t.Y-15);
        if(p.Disease){Round(g,Color.FromArgb(233,164,96,76),new(t.X+24,t.Y-12,10,12),2);TextAt(g,"!",t.X+26,t.Y-13,11,Cream,true);}
        else if(p.RipeDay>=0){Box(g,Color.FromArgb(235,237,211,143),t.X+26,t.Y-5,6,6);Line(g,Moss,1,t.X+27,t.Y-2,t.X+29,t.Y);Line(g,Moss,1,t.X+29,t.Y,t.X+32,t.Y-4);}
        else if(p.Moisture<25){Poly(g,Color.FromArgb(184,214,230),new(t.X+29,t.Y+23),new(t.X+26,t.Y+28),new(t.X+29,t.Y+31),new(t.X+32,t.Y+28));}
    }
    void CropArt(Graphics g,string crop,float x,float y,float s)=>art.Draw(g,art.Crops[art.CropIndex(crop),3],x-18*s,y-30*s,36*s,45*s);
    void RichTree(Graphics g,float x,float y,float s) {
        int season=world?.Season??0;float wind=reducedMotion||quality==0?0:(float)Math.Sin(VisualTime*(world?.Weather=="พายุ"?2.2:.9)+x)* (world?.Weather=="พายุ"?2.1f:.65f);
        SoftShadow(g,x+9*s,y+105*s,88*s,18*s);
        art.Draw(g,art.Trunk,x,y+87*s,94*s,30*s);art.Draw(g,art.Trees[season,(int)Math.Abs(x)%3],x+wind,y+10*s,94*s,100*s);
    }
    void RichHouse(Graphics g,float x,float y,float s,bool shop) {
        SoftShadow(g,x+7*s,y+111*s,148*s,26*s);art.Draw(g,art.Houses[shop?1:0],x,y,146*s,136*s);
        if(!reducedMotion&&quality>0)for(int i=0;i<3;i++){float phase=(float)((VisualTime*.17+i*.32)%1);Oval(g,Color.FromArgb((int)((1-phase)*48),237,235,213),x+(102+phase*7)*s,y+(6-phase*27)*s,(5+phase*9)*s,(5+phase*5)*s);}
    }
    void DrawCrate(Graphics g,float x,float y){SoftShadow(g,x+3,y+37,50,14);art.Draw(g,art.Crate,x,y);}
    void DrawFarmerSprite(Graphics g,float x,float y,AvatarPose a,int color,bool sleeping) {
        SoftShadow(g,x-13,y+3,28,10);float idle=reducedMotion?0:(float)Math.Sin(VisualTime*2+a.X)*.35f;
        int frame=a.Speed>.1&&!reducedMotion?(int)(a.Phase*1.6)%8:0;
        double action=a.ActionUntil>VisualTime?Math.Clamp((VisualTime-a.ActionStart)/.72,0,1):0;
        float bend=(float)Math.Sin(action*Math.PI)*3;
        art.Draw(g,art.Farmers[color%4,a.Facing,frame],x-18,y-54+idle+bend);
        if(action>0){int icon=a.Action switch {"water"=>1,"harvest"=>2,"treat"=>3,"fertilize"=>4,"stake"=>5,"clear"=>6,_=>0};var saved=g.Save();g.TranslateTransform(x+13,y-20-bend);g.RotateTransform((float)Math.Sin(action*Math.PI)*45-15);art.Draw(g,art.Icons[icon],-8,-8,20,20);g.Restore(saved);}
        if(sleeping)TextAt(g,"z Z",x+8,y-74,14,Cream,true);
    }
    void DrawPond(Graphics g,Farm f) {
        if(quality>0)for(int i=0;i<5;i++){float phase=reducedMotion?.35f:(float)((VisualTime*.12+i*.197)%1);float x=656+i*23%120,y=550+i*19%80;using var pen=new Pen(Color.FromArgb((int)((1-phase)*100),229,241,217),1);g.DrawEllipse(pen,x-phase*16,y-phase*4,14+phase*35,3+phase*9);}
        for(int i=0;i<5;i++)art.Draw(g,art.Reeds,624+i*30,622+i%2*9,18,30);
        for(int i=0;i<2;i++){float x=684+i*57+(reducedMotion?0:(float)Math.Sin(VisualTime*.2+i)*17),y=574+i*31+(reducedMotion?0:(float)Math.Sin(VisualTime*1.7+i));Oval(g,Color.FromArgb(80,228,240,220),x-4,y+4,27,5);Oval(g,Color.FromArgb(242,234,208),x,y-5,20,11);Box(g,Color.FromArgb(213,211,176),x+3,y-1,8,4);Oval(g,Color.FromArgb(84,113,76),x+13,y-10,9,9);Box(g,Gold,x+21,y-6,6,3);Box(g,Moss,x+19,y-7,1,1);}
        if(weatherBlend>.01&&quality>0)for(int i=0;i<(quality==2?9:4);i++){float phase=(float)((VisualTime*1.35+i*.217)%1),x=650+i*37%160,y=548+i*29%93;using var p=new Pen(Color.FromArgb((int)((1-phase)*95*weatherBlend),213,232,224),1);g.DrawEllipse(p,x-phase*9,y-phase*3,2+phase*18,1+phase*6);}
    }
    void DrawWind(Graphics g,Farm f) {
        if(reducedMotion||quality==0)return;Color blade=f.Season==3?Color.FromArgb(147,159,171,150):Color.FromArgb(117,88,118,68);
        for(int i=0;i<(quality==2?36:14);i++){float x=60+i*113%850,y=202+i*79%506;if(x>290&&x<530&&y<483)continue;if(x>620&&y>500)continue;float sway=(float)Math.Sin(VisualTime*(f.Weather=="พายุ"?3.2:1.4)+i)* (f.Weather=="พายุ"?4:1.3f);Line(g,blade,1,x,y,x-2+sway,y-6);Line(g,blade,1,x+2,y,x+3+sway,y-7);}
    }
    void DrawLighting(Graphics g,Farm f) {
        double minute=renderPreview?f.Minute:displayMinute;
        float dusk=(float)Math.Clamp((minute-980)/220,0,1),dawn=(float)Math.Clamp((500-minute)/150,0,1);
        var bounds=new RectangleF(cameraX,cameraY,SceneView.Width/zoom,SceneView.Height/zoom);
        // Compose ambient colours mathematically; blend the full scene only once.
        float nightAlpha=dusk*149/255f,dawnAlpha=dawn*27/255f,weatherAlpha=(float)weatherBlend*(f.Weather=="พายุ"?33:18)/255f;
        float baseAlpha=nightAlpha>0?nightAlpha:dawnAlpha;Color baseColor=nightAlpha>0?Color.FromArgb(27,43,73):Color.FromArgb(234,193,145);
        float ambientAlpha=baseAlpha+weatherAlpha*(1-baseAlpha);
        if(ambientAlpha>.002f){float fraction=weatherAlpha/ambientAlpha;Color ambient=Color.FromArgb((int)(ambientAlpha*255),(int)(baseColor.R*(1-fraction)+63*fraction),(int)(baseColor.G*(1-fraction)+87*fraction),(int)(baseColor.B*(1-fraction)+106*fraction));Box(g,ambient,bounds.X,bounds.Y,bounds.Width,bounds.Height);}
        if(quality==2&&!reducedMotion&&f.Weather=="แดดอ่อน"&&dusk<.4){float x=80+(float)(VisualTime*5%820);Oval(g,Color.FromArgb(11,233,239,198),x-250,150,320,570);}
        if(dusk>.1){WindowLight(g,136,220,24,18,dusk);WindowLight(g,726,223,20,15,dusk);}
    }
    void WindowLight(Graphics g,float x,float y,float w,float h,float intensity) {
        g.SmoothingMode=SmoothingMode.AntiAlias;
        if(quality>0)art.DrawFade(g,art.Glow,new((int)x-31,(int)y-24,(int)w+62,(int)h+58),intensity);
        Box(g,Color.FromArgb((int)(intensity*220),255,215,137),x,y,w,h);Box(g,Color.FromArgb(245,220,177),x+w/2-1,y,2,h);Box(g,Color.FromArgb(245,220,177),x,y+h/2,w,2);g.SmoothingMode=SmoothingMode.None;
    }
    void DrawAtmosphere(Graphics g,Farm f) {
        if(reducedMotion)return;
        if(weatherBlend>.02){int count=quality switch {0=>22,1=>48,_=>88};for(int i=0;i<count;i++){
            float depth=.65f+i%3*.28f,x=cameraX-15+(float)((i*137+VisualTime* (f.Weather=="พายุ"?95:43)*depth)%(SceneView.Width/zoom+35)),y=cameraY-18+(float)((i*83+VisualTime*330*depth)%(SceneView.Height/zoom+35));
            Line(g,Color.FromArgb((int)(weatherBlend*(65+i%3*27)),198,224,223),i%3==2?1.3f: .8f,x,y,x-(f.Weather=="พายุ"?8:3),y+10*depth);
            if(i%9==0&&quality>0){float phase=(float)((VisualTime*2+i*.27)%1);Line(g,Color.FromArgb((int)((1-phase)*85*weatherBlend),217,232,213),1,x-3*phase,y+13,x+3*phase,y+13);}
        }}
        if(f.Season==3){int count=quality switch {0=>12,1=>25,_=>45};for(int i=0;i<count;i++){float x=cameraX+(float)((i*137+Math.Sin(VisualTime*.5+i)*8+100)%(SceneView.Width/zoom)),y=cameraY+(float)((i*83+VisualTime*(15+i%3*7))%(SceneView.Height/zoom));Box(g,Color.FromArgb(182,246,250,232),x,y,2,2);}}
        else if(f.Season==2){int count=quality==2?12:5;for(int i=0;i<count;i++){float x=cameraX+(float)((i*137+VisualTime*17)%(SceneView.Width/zoom)),y=cameraY+(float)((i*87+VisualTime*23)%(SceneView.Height/zoom));Oval(g,Color.FromArgb(167,204,137,77),x,y,4,2);}}
        else if(quality>0)for(int i=0;i<3;i++){float x=95+i*165+(float)Math.Sin(VisualTime*.5+i)*30,y=385+i*37+(float)Math.Cos(VisualTime*.7+i)*18;float flap=(float)Math.Abs(Math.Sin(VisualTime*8+i))*4;Oval(g,Color.FromArgb(228,228,200,139),x-flap,y,flap+1,4);Oval(g,Color.FromArgb(225,219,156,138),x,y,flap+1,4);}
        if(quality==2&&f.Weather=="ฝนตก"&&f.Minute<990){float drift=(float)Math.Sin(VisualTime*.08)*60;Oval(g,Color.FromArgb(10,212,223,209),cameraX+drift,cameraY+270,SceneView.Width/zoom,100);}
    }
    void DrawSparks(Graphics g) {
        foreach(var p in sparks){double age=VisualTime-p.Start,t=Math.Clamp(age/p.Life,0,1);Color color=Color.FromArgb((int)((1-t)*230),p.Color);float x=(float)(p.X+p.Vx*age),y=(float)(p.Y+p.Vy*age+(p.Text!=""?0:55*age*age));
            if(p.Ring){using var pen=new Pen(color,1);g.DrawEllipse(pen,(float)p.X-(float)t*15,(float)p.Y-(float)t*6,3+(float)t*30,1+(float)t*12);}
            else if(p.Text!=""){TextAt(g,p.Text,x+1,y+1,12,Color.FromArgb(color.A,246,236,208),true);TextAt(g,p.Text,x,y,12,color,true);}
            else if(p.Crop!=""&&p.Destination is PointF end){float u=(float)t;x=(float)p.X+(end.X-(float)p.X)*u;y=(float)p.Y+(end.Y-(float)p.Y)*u-(float)Math.Sin(u*Math.PI)*44;CropArt(g,p.Crop,x,y,(float)(.85-t*.45));}
            else if(p.Coin){art.Draw(g,art.Icons[7],x-5,y-5,10,10);}
            else Box(g,color,x,y,p.Size,p.Size);
        }
    }
    void DrawInteractionHint(Graphics g,Farm f) {
        string hint=Farm.Distance(Me??new Farmer(),4,4)<=2?"Space · พักผ่อนที่บ้าน":Farm.Distance(Me??new Farmer(),20,8)<=2?"E · ขายผลผลิตในกระเป๋า": "";
        if(hint!=""&&uiPanel==""){var r=new RectangleF(491,639,298,32);Panel(g,r);TextAt(g,hint,r.X+12,r.Y+7,14,Moss,true);}
    }
    internal void SetQualityForTest(int value){quality=Math.Clamp(value,0,2);}
}
