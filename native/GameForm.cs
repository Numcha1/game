using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace SuanPleng;

public sealed partial class GameForm : Form {
    Farm? world;
    RoomServer? server;
    RoomClient? client;
    InternetTunnel? tunnel;
    readonly string session;
    readonly string savePath;
    readonly System.Windows.Forms.Timer frame=new(){Interval=16};
    readonly System.Windows.Forms.Timer network=new(){Interval=250};
    readonly HashSet<Keys> keys=new();
    readonly List<(RectangleF Area,Action Action)> buttons=new();
    readonly Dictionary<string,Font> fonts=new();
    readonly Queue<Request> pending=new();
    readonly Stopwatch elapsed=Stopwatch.StartNew();
    double lastFrame,px=6,py=4;
    bool busy,connecting,closed,connected;
    string toast="",invite="",name="ชาวสวน",seed="carrot",tool="plant",mode="",status="";
    int generation;
    DateTime toastUntil;
    float scale=1,ox,oy;
    static readonly Color Cream=Color.FromArgb(248,241,220),Moss=Color.FromArgb(45,70,45),Gold=Color.FromArgb(223,171,71);
    static readonly (string Key,string Label)[] Tools={ ("plant","1 · เมล็ด"),("water","2 · รดน้ำ"),("harvest","3 · เก็บเกี่ยว"),("treat","4 · รักษา"),("fertilize","5 · ปุ๋ย 10"),("stake","6 · ไม้ค้ำ 8"),("clear","7 · ถอนต้น") };
    public GameForm(string? dataDirectory=null) {
        Text="สวนเพลง · Seasons Together";ClientSize=new(1280,800);MinimumSize=new(980,640);DoubleBuffered=true;KeyPreview=true;
        AutoScaleMode=AutoScaleMode.None;BackColor=Moss;
        string data=dataDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SuanPleng");
        Directory.CreateDirectory(data);savePath=Path.Combine(data,"farm.json");
        string identity=Path.Combine(data,"player.txt");
        session=File.Exists(identity)?File.ReadAllText(identity).Trim():Guid.NewGuid().ToString("N");
        if(!Guid.TryParseExact(session,"N",out _))session=Guid.NewGuid().ToString("N");File.WriteAllText(identity,session);
        frame.Tick+=(_,_)=>{ double now=elapsed.Elapsed.TotalSeconds,dt=Math.Min(.05,now-lastFrame);lastFrame=now;MovePlayer(dt);Animate(dt);Invalidate(); };
        network.Tick+=async(_,_)=>await Poll();
        KeyDown+=OnKey;KeyUp+=(_,e)=>keys.Remove(e.KeyCode);Deactivate+=(_,_)=>keys.Clear();
        MouseDown+=OnClick;
        FormClosing+=(_,_)=>{closed=true;frame.Stop();network.Stop();client?.Dispose();server?.Dispose();tunnel?.Dispose();};
        frame.Start();
    }
    Farmer? Me=>world?.Players.FirstOrDefault(p=>p.Id==Farm.Identity(session));
    Font F(float size,bool bold=false) { string key=size+"/"+bold;if(!fonts.ContainsKey(key))fonts[key]=new Font("Tahoma",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);return fonts[key]; }
    static Brush B(Color c)=>new SolidBrush(c);
    void Box(Graphics g,Color c,float x,float y,float w,float h) { using var b=B(c);g.FillRectangle(b,x,y,w,h); }
    void Oval(Graphics g,Color c,float x,float y,float w,float h) {using var b=B(c);g.FillEllipse(b,x,y,w,h);}
    void TextAt(Graphics g,string text,float x,float y,float size=16,Color? color=null,bool bold=false,float width=0) {
        using var b=B(color??Moss);if(width>0)g.DrawString(text,F(size,bold),b,new RectangleF(x,y,width,120));else g.DrawString(text,F(size,bold),b,x,y);
    }
    void Button(Graphics g,string text,RectangleF r,Action action,bool active=false,bool disabled=false) {
        Round(g,Color.FromArgb(32,63,60,34),new(r.X+1,r.Y+3,r.Width,r.Height),5);
        Round(g,disabled?Color.FromArgb(216,218,196):active?Gold:Color.FromArgb(227,232,206),r,5);
        if(!disabled){Point mouse=PointToClient(Cursor.Position);if(r.Contains((mouse.X-ox)/scale,(mouse.Y-oy)/scale))Round(g,Color.FromArgb(24,255,255,241),r,5);}
        Line(g,Color.FromArgb(90,255,255,228),1,r.X+6,r.Y+1,r.Right-6,r.Y+1);
        TextAt(g,text,r.X+10,r.Y+8,15,disabled?Color.Gray:Moss,true);
        if(!disabled)buttons.Add((r,action));
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        scale=Math.Min(ClientSize.Width/1280f,ClientSize.Height/800f);ox=(ClientSize.Width-1280*scale)/2;oy=(ClientSize.Height-800*scale)/2;
        g.TranslateTransform(ox,oy);g.ScaleTransform(scale,scale);buttons.Clear();
        Box(g,Cream,0,0,1280,800);
        if(world==null)DrawMenu(g);else DrawGame(g);
        if(DateTime.UtcNow<toastUntil) { Box(g,Moss,240,680,730,88);TextAt(g,toast,260,693,18,Cream,false,690); }
        if(connecting) { Box(g,Color.FromArgb(236,223,186),220,650,850,75);TextAt(g,"กำลังเปิดห้อง / เชื่อมต่อ… โปรดรอ",245,666,23); }
    }
    void DrawMenu(Graphics g) {
        Box(g,Color.FromArgb(167,188,125),0,360,1280,440);
        for(int i=0;i<9;i++) {float x=i*160-40;Tree(g,x,310,1.8f);}
        TextAt(g,"สวนเพลง",90,80,58,Moss,true);TextAt(g,"SEASONS TOGETHER",95,153,22);
        TextAt(g,"ปลูกด้วยกัน เติบโตไปด้วยกัน",95,205,27);
        TextAt(g,"เกมฟาร์ม Windows · 4 ฤดู · 1–4 คน",95,255,18);
        House(g,150,420,2.2f,false);
        Button(g,"เล่นคนเดียว / เล่นเซฟต่อ",new(735,160,410,55),()=>_ = Host(false),disabled:connecting);
        Button(g,"สร้างห้องออนไลน์",new(735,235,410,55),()=>_ = Host(true),true,connecting);
        Button(g,"เข้าห้องเพื่อน",new(735,310,410,55),()=>_ = Join(),disabled:connecting);
        Button(g,"วิธีเล่นและระบบฤดู",new(735,385,410,55),Help);
        Button(g,"เริ่มฟาร์มใหม่ (สำรองเซฟเดิม)",new(735,460,410,55),NewFarm,disabled:connecting);
        TextAt(g,"เจ้าของห้องต้องเปิดเกมไว้\nเข้าจากคนละบ้านด้วยรหัสเชิญ\nบันทึกอัตโนมัติ / เวลาไม่เดินเมื่อปิดเกม",735,545,18,Moss,false,420);
        TextAt(g,"รุ่นทดลอง 0.2 · เดินด้วย WASD หรือปุ่มลูกศร",95,739,16);
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
        if(world==null||!connected||world.Paused||Me?.Sleeping!=false)return;
        if(Avatar(sessionId).ActionUntil>VisualTime)return;
        double dx=(keys.Contains(Keys.D)||keys.Contains(Keys.Right)?1:0)-(keys.Contains(Keys.A)||keys.Contains(Keys.Left)?1:0);
        double dy=(keys.Contains(Keys.S)||keys.Contains(Keys.Down)?1:0)-(keys.Contains(Keys.W)||keys.Contains(Keys.Up)?1:0);
        double length=Math.Sqrt(dx*dx+dy*dy);if(length==0)return;dx=dx/length*4*dt;dy=dy/length*4*dt;
        if(Farm.Walkable(px+dx,py))px+=dx;if(Farm.Walkable(px,py+dy))py+=dy;
    }
    void OnKey(object? sender,KeyEventArgs e) {
        if(keys.Contains(e.KeyCode))return;keys.Add(e.KeyCode);
        if(world==null)return;
        if(e.KeyCode==Keys.F2){reducedMotion=!reducedMotion;Notify(reducedMotion?"ลดการเคลื่อนไหวแล้ว":"เปิดแอนิเมชันเต็มแล้ว");}
        if(e.KeyCode==Keys.M){soundEnabled=!soundEnabled;Notify(soundEnabled?"เปิดเสียงการกระทำแล้ว":"ปิดเสียงการกระทำแล้ว");}
        if(e.KeyCode>=Keys.D1&&e.KeyCode<=Keys.D7)tool=Tools[(int)e.KeyCode-(int)Keys.D1].Key;
        if(e.KeyCode==Keys.Space)Queue(Me?.Sleeping==true?"wake":"sleep");
        if(e.KeyCode==Keys.E)Queue("sell");if(e.KeyCode==Keys.Escape)Queue("pause");
        if(e.KeyCode==Keys.F1)Help();
    }
    void OnClick(object? sender,MouseEventArgs e) {
        var p=new PointF((e.X-ox)/scale,(e.Y-oy)/scale);
        var button=buttons.LastOrDefault(b=>b.Area.Contains(p));if(button.Action!=null){button.Action();return;}
        if(world==null)return;
        for(int i=0;i<24;i++){var t=Farm.Tile(i);if(new RectangleF(32+t.X*35,112+t.Y*35,35,35).Contains(p)){Queue(tool,i);return;}}
    }
    void DrawGame(Graphics g) {
        var f=world!;
        TextAt(g,"สวนเพลง",28,18,29,Moss,true);
        TextAt(g,$"{Farm.SeasonNames[f.Season]} · วันที่ {f.SeasonDay}/14 · ปี {(f.Day-1)/56+1}",205,18,20,Moss,true);
        TextAt(g,$"{(int)f.Minute/60:00}:{(int)f.Minute%60:00}   {f.Weather}   พรุ่งนี้: {f.Forecast(f.Day+1)}",205,49,16);
        TextAt(g,$"เหรียญกองกลาง {f.Coins}   |   แรง {Me?.Energy??0}/100",690,20,19,Moss,true);
        TextAt(g,$"{mode} · ออนไลน์ {f.Players.Count(p=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-p.LastSeen<30000)}/4",690,52,15);
        Button(g,"ช่วยเหลือ F1",new(1085,18,165,40),Help);
        TextAt(g,"WASD เดิน · คลิกแปลงใช้เครื่องมือ · E ขาย · Space นอน · Esc หยุดเวลา · F2 ลดแอนิเมชัน · M เสียง",30,84,15);
        DrawScene(g,f);
        // The right-hand panel is native game HUD, not a browser.
        Round(g,Color.FromArgb(24,60,55,32),new(931,116,325,630),9);
        Round(g,Color.FromArgb(232,225,196),new(928,112,325,630),9);
        TextAt(g,"เมล็ดพืช · คลิกเลือก",943,123,17,Moss,true);
        int k=0;foreach(var pair in Farm.Crops) {
            string key=pair.Key;var crop=pair.Value;bool suitable=f.Greenhouse||crop.Seasons.Contains(f.Season);
            var r=new RectangleF(943+k%2*150,154+k/2*47,145,43);
            Button(g,$"{crop.Name} {crop.Cost}◉",r,()=>{seed=key;tool="plant";},seed==key,!suitable);
            k++;
        }
        var selected=Farm.Crops[seed];
        TextAt(g,$"โต {selected.Days} วัน · เก็บทัน {selected.Window} วัน\nราคาวันนี้ {f.SalePrice(seed)} / ชิ้น",943,349,14,Moss,false,298);
        if(!f.Greenhouse&&selected.Days>14-f.SeasonDay)TextAt(g,"ระวัง! อาจเก็บไม่ทันฤดู",943,390,15,Color.FromArgb(175,63,40),true);
        k=0;foreach(var entry in Tools){string action=entry.Key;Button(g,entry.Label,new(943+k%2*150,420+k/2*38,145,34),()=>tool=action,tool==action);k++;}
        Button(g,"ขายผลผลิต · E",new(943,578,145,36),()=>Queue("sell"));Button(g,"นอน · Space",new(1093,578,145,36),()=>Queue(Me?.Sleeping==true?"wake":"sleep"));
        Button(g,"หลังคา 300◉",new(943,620,145,36),()=>Queue("greenhouse"));Button(g,"ทุนฉุกเฉิน",new(1093,620,145,36),()=>Queue("rescue"));
        Button(g,"YouTube เพลง",new(943,663,145,36),Music);Button(g,"รหัสเชิญ",new(1093,663,145,36),ShowInvite,disabled:invite=="");
        Button(g,"ออก / บันทึก",new(943,706,295,31),()=>{if(MessageBox.Show("ออกจากฟาร์ม? หากคุณเป็นเจ้าของ ห้องเพื่อนจะปิดด้วย","สวนเพลง",MessageBoxButtons.YesNo)==DialogResult.Yes)LeaveFarm();});
        TextAt(g,$"กระเป๋า: {(Me?.Bag.Count>0?string.Join(" · ",Me.Bag.Select(b=>Farm.Crops[b.Key].Name+" "+b.Value)):"ว่าง")} · ผลผลิตลด 25% ข้ามคืน",30,751,15);
        if(status!="")TextAt(g,status,30,775,14,Color.FromArgb(170,50,35));else TextAt(g,f.Paused?"หยุดเวลาอยู่ · เจ้าของกด Esc เพื่อเล่นต่อ":f.Journal.FirstOrDefault()??"",30,775,14);
        // Hover inspection tells the player why a crop needs attention.
        Point cursor=PointToClient(Cursor.Position);float hx=(cursor.X-ox)/scale,hy=(cursor.Y-oy)/scale;
        for(int i=0;i<24;i++){var t=Farm.Tile(i);if(new RectangleF(32+t.X*35,112+t.Y*35,35,35).Contains(hx,hy)){var p=f.Plots[i];string info=$"แปลง {i+1} · น้ำ {p.Moisture} · ดิน {p.Fertility}\n"+(p.Crop==""?"ว่าง · แปลงพักฟื้นดินวันละ 3":$"{Farm.Crops[p.Crop].Name} โต {p.Growth:0.#}/{Farm.Crops[p.Crop].Days} · คุณภาพ {p.Quality}\n"+(p.RipeDay>=0?$"พร้อมเก็บ เหลือ {Farm.Crops[p.Crop].Window-(f.Day-p.RipeDay)} วัน":p.Disease?"ป่วย! รักษาก่อนโรคลาม":"ต้องรดน้ำทุกวัน"));Box(g,Cream,Math.Min(hx+15,580),Math.Min(hy+15,590),325,82);TextAt(g,info,Math.Min(hx+25,590),Math.Min(hy+23,598),14,Moss,false,305);break;}}
    }
    void House(Graphics g,float x,float y,float s,bool shop) => RichHouse(g,x,y,s,shop);
    void Tree(Graphics g,float x,float y,float s) => RichTree(g,x,y,s);
    void Notify(string message){toast=message;toastUntil=DateTime.UtcNow.AddSeconds(5);}
    void Help()=>MessageBox.Show("WASD / ลูกศร: เดิน  |  คลิกแปลงใกล้ตัว: ใช้เครื่องมือ\n1 เมล็ด  2 น้ำ  3 เก็บ  4 รักษา  5 ปุ๋ย  6 ไม้ค้ำ  7 ถอน\nE: ขายที่ลัง  |  Space: นอนหน้าบ้าน  |  Esc: เจ้าของหยุดเวลา\n\nหนึ่งวันประมาณ 10 นาที ทุกคนเข้านอนเพื่อข้ามวัน\nฤดูละ 14 วัน พืชผิดฤดูตายเมื่อเปลี่ยนฤดู\nรดน้ำทุกวัน ดินแห้งหยุดโต ดินแฉะในฝนทำให้รากเน่า\nไม้ค้ำป้องกันพายุ ปุ๋ยฟื้นดิน ปลูกซ้ำดินเสื่อม\nเก็บผักภายใน 1–3 วัน ผลผลิตในกระเป๋าลด 25% ทุกเช้า\nเงินกองกลาง หลังคาเรือนกระจกให้เจ้าของอนุมัติที่ร้าน\n\nออนไลน์: เจ้าของเปิดห้องแล้วส่งรหัสเชิญเต็มให้เพื่อน\nเปิดห้องใหม่รหัสจะเปลี่ยน ส่วนเชื่อมต่อรวมอยู่ในเกมแล้ว\nเวลาหยุดเมื่อปิดเกม เซฟอยู่ใน LocalAppData\\SuanPleng\nYouTube เปิดในเบราว์เซอร์แยกเพื่อฟังระหว่างเล่น","คู่มือสวนเพลง");
    void ShowInvite(){if(invite==""){Notify("รหัสเชิญมีเฉพาะห้องออนไลน์");return;}Ask("รหัสเชิญ · คัดลอกทั้งหมดส่งให้เพื่อน","เลือกข้อความทั้งหมดแล้ว Ctrl+C (รหัสเปลี่ยนเมื่อเปิดห้องใหม่)",invite);}
    void Music(){string? link=Ask("วิทยุ YouTube","ใส่ลิงก์ YouTube · จะเปิดเพลงในเบราว์เซอร์","");if(link==null)return;if(!Uri.TryCreate(link.Trim(),UriKind.Absolute,out var u)||u.Scheme!="https"||u.UserInfo!=""||!u.IsDefaultPort||!new[]{"youtube.com","www.youtube.com","music.youtube.com","m.youtube.com","youtu.be"}.Contains(u.Host)){Notify("ใส่ลิงก์ https:// ของ YouTube เท่านั้น");return;}try{Process.Start(new ProcessStartInfo(u.AbsoluteUri){UseShellExecute=true});}catch(Exception){Notify("เปิดเบราว์เซอร์ไม่ได้ กรุณาเปิดลิงก์เอง");}}
    void NewFarm(){if(MessageBox.Show("เริ่มใหม่? เซฟปัจจุบันจะเก็บเป็นไฟล์สำรอง","สวนเพลง",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;try{if(File.Exists(savePath))File.Move(savePath,savePath+"."+DateTime.Now.ToString("yyyyMMddHHmmss")+".bak");Notify("พร้อมเริ่มฟาร์มใหม่แล้ว");}catch(IOException e){Notify(e.Message);}}
    void LeaveFarm(){generation++;network.Stop();client?.Dispose();client=null;server?.Dispose();server=null;tunnel?.Dispose();tunnel=null;world=null;connected=false;pending.Clear();invite="";keys.Clear();ResetVisuals();}
    string? Ask(string title,string label,string initial) {
        keys.Clear();using var dialog=new Form{Text=title,ClientSize=new(610,165),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Cream};
        var hint=new Label{Text=label,Location=new(18,15),Size=new(575,35),Font=new("Tahoma",10)};
        var input=new TextBox{Text=initial,Location=new(18,55),Size=new(575,30),Font=new("Tahoma",11),MaxLength=512};
        var ok=new Button{Text="ตกลง",Location=new(380,110),Size=new(100,34),DialogResult=DialogResult.OK};
        var cancel=new Button{Text="ยกเลิก",Location=new(490,110),Size=new(100,34),DialogResult=DialogResult.Cancel};
        dialog.Controls.AddRange(new Control[]{hint,input,ok,cancel});dialog.AcceptButton=ok;dialog.CancelButton=cancel;dialog.Shown+=(_,_)=>{input.Focus();input.SelectAll();};
        return dialog.ShowDialog(this)==DialogResult.OK?input.Text:null;
    }
    public Bitmap RenderForTest(){ClientSize=new(1280,800);CreateControl();var b=new Bitmap(Width,Height);DrawToBitmap(b,new Rectangle(0,0,Width,Height));return b;}
    public void TestWorld(Farm farm){farm.Players.Clear();farm.Owner="";farm.Join(session,"ชาวสวน");world=farm;px=6;py=6;mode="ตรวจภาพ";}
    protected override void Dispose(bool disposing){if(disposing){frame.Dispose();network.Dispose();foreach(var f in fonts.Values)f.Dispose();foreach(var s in sounds.Values){var stream=s.Stream;s.Dispose();stream?.Dispose();}}base.Dispose(disposing);}
}
