using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace SuanPleng;

public sealed partial class GameForm : Form {
    Farm? world;
    RoomServer? server;
    RoomClient? client;
    InternetTunnel? tunnel;
    readonly string session;
    readonly string publicPlayerId;
    readonly string savePath;
    readonly FramePulse frame;
    readonly FrameClock frameClock=new();
    internal event Action? FrameStep;
    readonly System.Windows.Forms.Timer network=new(){Interval=250};
    readonly HashSet<Keys> keys=new();
    readonly List<(RectangleF Area,Action Action)> buttons=new();
    readonly Dictionary<(float,bool),Font> fonts=new();
    readonly Queue<Request> pending=new();
    readonly Stopwatch elapsed=Stopwatch.StartNew();
    double lastFrame,px=6,py=4;
    bool busy,connecting,closed,connected,graphicsDisposed;
    string toast="",invite="",name="ชาวสวน",seed="carrot",tool="plant",mode="",status="";
    int generation;
    DateTime toastUntil;
    float scale=1,ox,oy;
    Bitmap? presentationBuffer;
    Graphics? presentationGraphics;
    static readonly Color Cream=Color.FromArgb(248,241,220),Moss=Color.FromArgb(45,70,45),Gold=Color.FromArgb(223,171,71);
    static readonly (string Key,string Label)[] Tools={ ("plant","1 · เมล็ด"),("water","2 · รดน้ำ"),("harvest","3 · เก็บเกี่ยว"),("treat","4 · รักษา"),("fertilize","5 · ปุ๋ย 10"),("stake","6 · ไม้ค้ำ 8"),("clear","7 · ถอนต้น") };
    public GameForm(string? dataDirectory=null) {
        frame=new FramePulse(this);
        Text="สวนเพลง · Seasons Together";ClientSize=new(1280,800);MinimumSize=new(800,560);DoubleBuffered=true;KeyPreview=true;
        AutoScaleMode=AutoScaleMode.None;BackColor=Moss;
        string data=dataDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SuanPleng");
        Directory.CreateDirectory(data);LoadVisualPreferences(data);savePath=Path.Combine(data,"farm.json");
        string identity=Path.Combine(data,"player.txt");
        session=File.Exists(identity)?File.ReadAllText(identity).Trim():Guid.NewGuid().ToString("N");
        if(!Guid.TryParseExact(session,"N",out _))session=Guid.NewGuid().ToString("N");File.WriteAllText(identity,session);
        publicPlayerId=Farm.Identity(session);
        frame.Tick+=(_,_)=>{ double now=elapsed.Elapsed.TotalSeconds,dt=Math.Min(.05,now-lastFrame);lastFrame=now;MovePlayer(dt);Animate(dt);Invalidate();FrameStep?.Invoke(); };
        network.Tick+=async(_,_)=>await Poll();
        KeyDown+=OnKey;KeyUp+=(_,e)=>keys.Remove(e.KeyCode);Deactivate+=(_,_)=>keys.Clear();
        MouseDown+=OnClick;
        FormClosing+=(_,_)=>{closed=true;frame.Stop();network.Stop();client?.Dispose();server?.Dispose();tunnel?.Dispose();};
        frame.Start();
    }
    Farmer? Me=>world?.Players.FirstOrDefault(p=>p.Id==publicPlayerId);
    Font F(float size,bool bold=false) { var key=(size,bold);if(!fonts.TryGetValue(key,out var font)){font=new Font("Tahoma",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);fonts[key]=font;}return font; }
    static Brush B(Color c)=>new SolidBrush(c);
    void Box(Graphics g,Color c,float x,float y,float w,float h) => g.FillRectangle(CachedBrush(c),x,y,w,h);
    void Oval(Graphics g,Color c,float x,float y,float w,float h) => g.FillEllipse(CachedBrush(c),x,y,w,h);
    void TextAt(Graphics g,string text,float x,float y,float size=16,Color? color=null,bool bold=false,float width=0) {
        var b=CachedBrush(color??Moss);if(width>0)g.DrawString(text,F(size,bold),b,new RectangleF(x,y,width,120));else g.DrawString(text,F(size,bold),b,x,y);
    }
    void Button(Graphics g,string text,RectangleF r,Action action,bool active=false,bool disabled=false) {
        Round(g,Color.FromArgb(32,63,60,34),new(r.X+1,r.Y+3,r.Width,r.Height),5);
        Round(g,disabled?Color.FromArgb(224,217,192):active?Gold:Color.FromArgb(233,224,193),r,5);
        if(!disabled){Point mouse=PointToClient(Cursor.Position);if(r.Contains((mouse.X-ox)/scale,(mouse.Y-oy)/scale))Round(g,Color.FromArgb(24,255,255,241),r,5);}
        Line(g,Color.FromArgb(90,255,255,228),1,r.X+6,r.Y+1,r.Right-6,r.Y+1);
        if(text.Length>0)TextAt(g,text,r.X+10,r.Y+8,15,disabled?Color.Gray:Moss,true);
        if(!disabled)buttons.Add((r,action));
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        scale=Math.Min(ClientSize.Width/1280f,ClientSize.Height/800f);ox=(ClientSize.Width-1280*scale)/2;oy=(ClientSize.Height-800*scale)/2;
        bool upscale=scale>1;
        if(upscale&&presentationBuffer==null){presentationBuffer=new Bitmap(1280,800,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);presentationGraphics=Graphics.FromImage(presentationBuffer);}
        var g=upscale?presentationGraphics!:e.Graphics;var state=g.Save();
        try {
            g.SmoothingMode=SmoothingMode.AntiAlias;
            if(!upscale){g.TranslateTransform(ox,oy);g.ScaleTransform(scale,scale);}buttons.Clear();
            Box(g,Color.FromArgb(227,217,185),0,0,1280,800);
            if(world==null)DrawMenu(g);else DrawGame(g);
            if(DateTime.UtcNow<toastUntil) {Panel(g,new(329,591,622,85));TextAt(g,toast,347,606,17,Moss,false,587);}
            if(connecting) {Panel(g,new(329,588,622,83));TextAt(g,"กำลังเปิดห้อง / เชื่อมต่อ… โปรดรอ",352,611,22);}
        } finally {g.Restore(state);}
        if(upscale){var output=e.Graphics;var outputState=output.Save();output.InterpolationMode=InterpolationMode.NearestNeighbor;output.PixelOffsetMode=PixelOffsetMode.Half;output.DrawImage(presentationBuffer!,new RectangleF(ox,oy,1280*scale,800*scale),new RectangleF(0,0,1280,800),GraphicsUnit.Pixel);output.Restore(outputState);}
    }
    SolidBrush CachedBrush(Color color) {
        int key=color.ToArgb();if(brushCache.TryGetValue(key,out var brush))return brush;
        if(brushCache.Count>=512){foreach(var item in brushCache.Values)item.Dispose();brushCache.Clear();}
        brush=new SolidBrush(color);brushCache[key]=brush;return brush;
    }
    async Task Host(bool online) {
        if(connecting)return;connecting=true;
        try {
            var ask=Ask("ชื่อชาวสวน","ชื่อของคุณ (ไม่เกิน 18 ตัวอักษร)",name);if(ask==null)return;name=ask;
            var farm=Farm.Load(savePath);server=new(farm,savePath);server.ClaimOwner(session);
            client=new(server.Url,server.Token);
            var reply=await client.Send(new Request{Session=session,Name=name,Action="join"});Apply(reply);
            mode=online?"เจ้าของห้องออนไลน์":"เล่นคนเดียว";
            if(online) { tunnel=new();string address=await tunnel.Start(server.Port);invite=InternetTunnel.Invite(address,server.Token);ShowInvite(); }
            connected=true;network.Start();Notify("พร้อมแล้ว! เดินใกล้แปลง เลือกเมล็ด แล้วคลิกเพื่อปลูก");
        } catch(Exception e) { LeaveFarm();Notify("เปิดฟาร์มไม่ได้: "+Friendly(e)); }
        finally{connecting=false;}
    }
    async Task Join() {
        if(connecting)return;
        var code=Ask("เข้าห้องเพื่อน","วางรหัสเชิญเต็มที่เจ้าของฟาร์มส่งให้","");if(code==null)return;
        var ask=Ask("ชื่อชาวสวน","ชื่อของคุณ",name);if(ask==null)return;name=ask;connecting=true;
        try {
            var parsed=InternetTunnel.Parse(code);client=new(parsed.Url,parsed.Token);
            Apply(await client.Send(new Request{Session=session,Name=name,Action="join"}));
            mode="เล่นกับเพื่อน";connected=true;network.Start();Notify("เข้าฟาร์มแล้ว ทุกคนใช้ฤดูและเงินกองกลางเดียวกัน");
        } catch(Exception e) { LeaveFarm();Notify("เข้าห้องไม่ได้: "+Friendly(e)); }
        finally{connecting=false;}
    }
    static string Friendly(Exception e)=>e is TimeoutException or TaskCanceledException ? "หมดเวลารอ ลองอีกครั้งและตรวจอินเทอร์เน็ต" : e is HttpRequestException ? "ติดต่อเจ้าของไม่ได้ ตรวจว่าเจ้าของยังเปิดห้องอยู่" : e.Message;
    void Apply(Reply reply) {
        if(reply.Farm==null)throw new IOException(reply.Message);
        bool dayChanged=world!=null && world.Day!=reply.Farm.Day;
        if(world==null){uiPanel="";cameraReady=false;displaySeason=-1;}
        if(dayChanged){CapturePreviousDay();cameraReady=false;}
        ReceiveEffects(reply.Farm,world==null,dayChanged);
        world=reply.Farm;var me=Me;if(me!=null && (!connected || dayChanged || Farm.Distance(me,px,py)>2.5 || me.Sleeping)) {px=me.X;py=me.Y;}
        if(reply.Message!="")Notify(reply.Message);status="";
    }
    async Task Poll() {
        if(busy||client==null||closed)return;busy=true;
        int roomGeneration=generation;var connection=client;
        try {
            var r=pending.Count>0?pending.Peek():new Request {Action="sync"};r.Session=session;r.X=px;r.Y=py;
            var reply=await connection.Send(r);
            if(closed||roomGeneration!=generation)return;
            Apply(reply);connected=true;
            if(pending.Count>0 && ReferenceEquals(pending.Peek(),r))pending.Dequeue();
        } catch(Exception e) {if(roomGeneration==generation&&!closed){connected=false;status=Friendly(e)+" · กำลังลองเชื่อมต่อใหม่";}}
        finally{busy=false;}
    }
    void Queue(string action,int index=-1) {
        if(!connected){Notify("การเชื่อมต่อขาดอยู่ รอเชื่อมต่อใหม่ก่อน");return;}
        if(pending.Count>=2){Notify("กำลังรอคำสั่งก่อนหน้า");return;}
        pending.Enqueue(new(){Action=action,Index=index,Seed=seed,Command=Guid.NewGuid().ToString("N")});_ = Poll();
    }
    void MovePlayer(double dt) {
        if(uiPanel!=""||world==null||!connected||world.Paused||Me?.Sleeping!=false)return;
        if(Avatar(sessionId).ActionUntil>VisualTime)return;
        double dx=(keys.Contains(Keys.D)||keys.Contains(Keys.Right)?1:0)-(keys.Contains(Keys.A)||keys.Contains(Keys.Left)?1:0);
        double dy=(keys.Contains(Keys.S)||keys.Contains(Keys.Down)?1:0)-(keys.Contains(Keys.W)||keys.Contains(Keys.Up)?1:0);
        double length=Math.Sqrt(dx*dx+dy*dy);if(length==0)return;dx=dx/length*4*dt;dy=dy/length*4*dt;
        if(Farm.Walkable(px+dx,py))px+=dx;if(Farm.Walkable(px,py+dy))py+=dy;
    }
    void OnKey(object? sender,KeyEventArgs e) {
        if(keys.Contains(e.KeyCode))return;keys.Add(e.KeyCode);
        if(e.KeyCode==Keys.F2){reducedMotion=!reducedMotion;SaveVisualPreferences();Notify(reducedMotion?"ลดการเคลื่อนไหวแล้ว":"เปิดแอนิเมชันเต็มแล้ว");}
        if(e.KeyCode==Keys.M){soundEnabled=!soundEnabled;SaveVisualPreferences();Notify(soundEnabled?"เปิดเสียงการกระทำแล้ว":"ปิดเสียงการกระทำแล้ว");}
        if(e.KeyCode==Keys.F3)OpenPanel("settings");
        if(e.KeyCode==Keys.F1)Help();
        if(world==null)return;
        if(e.KeyCode==Keys.Tab){OpenPanel("seeds");e.SuppressKeyPress=true;return;}
        if(e.KeyCode==Keys.B||e.KeyCode==Keys.I)OpenPanel("bag");
        if(e.KeyCode==Keys.O)OpenPanel("market");
        if(e.KeyCode==Keys.Oemplus||e.KeyCode==Keys.Add){zoom=Math.Min(1.7f,zoom+.15f);cameraReady=false;SaveVisualPreferences();}
        if(e.KeyCode==Keys.OemMinus||e.KeyCode==Keys.Subtract){zoom=Math.Max(1,zoom-.15f);cameraReady=false;SaveVisualPreferences();}
        if(e.KeyCode==Keys.Escape){
            if(uiPanel!=""){bool pausePanel=uiPanel=="pause";uiPanel="";if(pausePanel&&world.Paused&&Me?.Id==world.Owner)Queue("pause");}
            else {Queue("pause");uiPanel="pause";}
            keys.Clear();return;
        }
        if(uiPanel!="")return;
        if(e.KeyCode>=Keys.D1&&e.KeyCode<=Keys.D7)tool=Tools[(int)e.KeyCode-(int)Keys.D1].Key;
        if(e.KeyCode==Keys.Space)Queue(Me?.Sleeping==true?"wake":"sleep");
        if(e.KeyCode==Keys.E)Queue("sell");
    }
    void OnClick(object? sender,MouseEventArgs e) {
        var p=new PointF((e.X-ox)/scale,(e.Y-oy)/scale);
        var button=buttons.LastOrDefault(b=>b.Area.Contains(p));if(button.Action!=null){button.Action();return;}
        if(world==null||uiPanel!=""||!SceneView.Contains(p))return;
        var at=SceneToWorld(p);
        for(int i=0;i<24;i++){var t=TilePoint(i);if(new RectangleF(t.X,t.Y,35,35).Contains(at)){Queue(tool,i);return;}}
    }
    void House(Graphics g,float x,float y,float s,bool shop) => RichHouse(g,x,y,s,shop);
    void Tree(Graphics g,float x,float y,float s) => RichTree(g,x,y,s);
    void Notify(string message){toast=message;toastUntil=DateTime.UtcNow.AddSeconds(5);}
    void Help()=>OpenPanel("help");
    void ShowInvite(){if(invite==""){Notify("รหัสเชิญมีเฉพาะห้องออนไลน์");return;}Ask("รหัสเชิญ · คัดลอกทั้งหมดส่งให้เพื่อน","เลือกข้อความทั้งหมดแล้ว Ctrl+C (รหัสเปลี่ยนเมื่อเปิดห้องใหม่)",invite);}
    void Music(){string? link=Ask("วิทยุ YouTube","ใส่ลิงก์ YouTube · จะเปิดเพลงในเบราว์เซอร์","");if(link==null)return;if(!Uri.TryCreate(link.Trim(),UriKind.Absolute,out var u)||u.Scheme!="https"||u.UserInfo!=""||!u.IsDefaultPort||!new[]{"youtube.com","www.youtube.com","music.youtube.com","m.youtube.com","youtu.be"}.Contains(u.Host)){Notify("ใส่ลิงก์ https:// ของ YouTube เท่านั้น");return;}try{Process.Start(new ProcessStartInfo(u.AbsoluteUri){UseShellExecute=true});}catch(Exception){Notify("เปิดเบราว์เซอร์ไม่ได้ กรุณาเปิดลิงก์เอง");}}
    void NewFarm(){if(MessageBox.Show("เริ่มใหม่? เซฟปัจจุบันจะเก็บเป็นไฟล์สำรอง","สวนเพลง",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;try{if(File.Exists(savePath))File.Move(savePath,savePath+"."+DateTime.Now.ToString("yyyyMMddHHmmss")+".bak");Notify("พร้อมเริ่มฟาร์มใหม่แล้ว");}catch(IOException e){Notify(e.Message);}}
    void LeaveFarm(){generation++;network.Stop();client?.Dispose();client=null;server?.Dispose();server=null;tunnel?.Dispose();tunnel=null;world=null;uiPanel="";cameraReady=false;displaySeason=-1;connected=false;pending.Clear();invite="";keys.Clear();ResetVisuals();}
    string? Ask(string title,string label,string initial) {
        using var dialogFont=new Font("Tahoma",11);
        keys.Clear();using var dialog=new Form{Text=title,ClientSize=new(610,165),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Cream};
        var hint=new Label{Text=label,Location=new(18,15),Size=new(575,35),Font=dialogFont};
        var input=new TextBox{Text=initial,Location=new(18,55),Size=new(575,30),Font=dialogFont,MaxLength=512};
        var ok=new Button{Text="ตกลง",Location=new(380,110),Size=new(100,34),DialogResult=DialogResult.OK};
        var cancel=new Button{Text="ยกเลิก",Location=new(490,110),Size=new(100,34),DialogResult=DialogResult.Cancel};
        foreach(var button in new[]{ok,cancel}){button.Font=dialogFont;button.FlatStyle=FlatStyle.Flat;button.BackColor=Paper;button.ForeColor=Moss;button.FlatAppearance.BorderColor=Walnut;}
        dialog.Controls.AddRange(new Control[]{hint,input,ok,cancel});dialog.AcceptButton=ok;dialog.CancelButton=cancel;dialog.Shown+=(_,_)=>{input.Focus();input.SelectAll();};
        return dialog.ShowDialog(this)==DialogResult.OK?input.Text:null;
    }
    public Bitmap RenderForTest(){ClientSize=new(1280,800);CreateControl();var b=new Bitmap(Width,Height);DrawToBitmap(b,new Rectangle(0,0,Width,Height));return b;}
    public void TestWorld(Farm farm){farm.Players.Clear();farm.Owner="";farm.Join(session,"ชาวสวน");world=farm;px=6;py=6;mode="ตรวจภาพ";}
    protected override void Dispose(bool disposing){if(disposing&&!graphicsDisposed){graphicsDisposed=true;if(!closed){closed=true;generation++;client?.Dispose();server?.Dispose();tunnel?.Dispose();}frame.Dispose();network.Dispose();frameClock.Dispose();DisposeHudCache();previousDayLayer?.Dispose();presentationGraphics?.Dispose();presentationBuffer?.Dispose();art.Dispose();foreach(var brush in brushCache.Values)brush.Dispose();brushCache.Clear();foreach(var f in fonts.Values)f.Dispose();foreach(var s in sounds.Values){var stream=s.Stream;s.Dispose();stream?.Dispose();}}base.Dispose(disposing);}
}
