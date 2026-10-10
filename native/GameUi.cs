using System.Drawing.Drawing2D;

namespace SuanPleng;
public sealed partial class GameForm {
    string uiPanel="";
    static readonly Color Walnut=Color.FromArgb(111,83,59),Paper=Color.FromArgb(248,237,209),Muted=Color.FromArgb(126,125,95);
    static readonly string[] ToolNames={"เมล็ด","รดน้ำ","เก็บเกี่ยว","รักษา","ใส่ปุ๋ย","ไม้ค้ำ","ถอนต้น"};
    void Panel(Graphics g,RectangleF r,bool dark=false) {
        Round(g,Color.FromArgb(35,64,51,35),new(r.X+2,r.Y+4,r.Width,r.Height),8);
        Round(g,Walnut,r,8);Round(g,dark?Color.FromArgb(76,91,68):Paper,new(r.X+2,r.Y+2,r.Width-4,r.Height-4),6);
        Line(g,Color.FromArgb(228,253,244,217),1,r.X+9,r.Y+3,r.Right-9,r.Y+3);
    }
    void DrawIcon(Graphics g,int index,float x,float y,float size=28){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;art.Draw(g,art.Icons[index],x,y,size,size);}
    void UiButton(Graphics g,string label,RectangleF r,Action action,bool selected=false,bool disabled=false,int icon=-1) {
        Button(g,"",r,action,selected,disabled);
        if(icon>=0)DrawIcon(g,icon,r.X+7,r.Y+5,25);
        TextAt(g,label,r.X+(icon>=0?38:12),r.Y+8,14,disabled?Muted:Moss,true);
    }
    void OpenPanel(string panel){keys.Clear();buttons.Clear();uiPanel=uiPanel==panel?"":panel;Invalidate();}
    void DrawMenu(Graphics g) {
        var saved=g.Save();g.SetClip(new Rectangle(0,0,1280,800));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
        g.DrawImage(art.Ground[0],new Rectangle(0,0,1280,1067));
        RichTree(g,25,230,2.1f);RichTree(g,427,407,1.9f);RichTree(g,103,535,1.5f);RichHouse(g,193,345,2.5f,false);
        for(int i=0;i<5;i++){float x=216+i*67;SoftShadow(g,x+10,677,46,12);art.Draw(g,art.Soils[1],x,653,54,54);art.Draw(g,art.Crops[i,3],x,620,54,67);}
        Box(g,Color.FromArgb(30,228,215,169),0,0,1280,800);g.Restore(saved);
        Panel(g,new(65,49,506,217));TextAt(g,"สวนเพลง",97,69,56,Moss,true);TextAt(g,"S E A S O N S   T O G E T H E R",100,141,17,Walnut,true);TextAt(g,"ฟาร์มเล็ก ๆ กับฤดูกาลที่เติบโตไปด้วยกัน",100,184,18);TextAt(g,"สี่ฤดู · ปลูกผัก · เล่นกับเพื่อน 1–4 คน",101,222,15,Muted);
        Panel(g,new(729,89,462,577));TextAt(g,"กลับบ้านที่ฟาร์ม",765,116,29,Moss,true);TextAt(g,"เลือกการเดินทางของชาวสวน",766,160,16,Muted);
        UiButton(g,"เล่นคนเดียว / เล่นเซฟต่อ",new(765,207,390,53),()=>_ = Host(false),disabled:connecting,icon:0);
        UiButton(g,"สร้างห้องออนไลน์",new(765,274,390,53),()=>_ = Host(true),true,connecting,11);
        UiButton(g,"เข้าห้องเพื่อน",new(765,341,390,53),()=>_ = Join(),disabled:connecting,icon:11);
        UiButton(g,"คู่มือชาวสวน",new(765,408,190,44),Help,icon:0);UiButton(g,"ตั้งค่าภาพ",new(966,408,189,44),()=>OpenPanel("settings"),icon:10);
        UiButton(g,"เริ่มฟาร์มใหม่ · สำรองเซฟเดิม",new(765,469,390,42),NewFarm,disabled:connecting);
        TextAt(g,"เซฟอัตโนมัติ · เวลาไม่เดินเมื่อปิดเกม\nเจ้าของห้องต้องเปิดเกมไว้ระหว่างเล่นออนไลน์\nใช้รหัสเชิญเพื่อเข้าจากคนละบ้าน",765,546,15,Muted,false,390);
        TextAt(g,"รุ่น 0.3 · Cozy Graphics Update",75,745,15,Paper,true);
        if(uiPanel!="")DrawUiPanel(g);
    }
    void DrawGame(Graphics g) {
        var f=world!;DrawScene(g,f);DrawCachedHud(g,f);DrawFooter(g,f);
        if(uiPanel!="")DrawUiPanel(g);else DrawPlotInspection(g,f);
    }
    void DrawHeader(Graphics g,Farm f) {
        Panel(g,new(16,12,416,64));DrawIcon(g,10,27,26,35);TextAt(g,$"{Farm.SeasonNames[f.Season]}  ·  {f.SeasonDay}/14",71,19,19,Moss,true);
        TextAt(g,$"ปี {(f.Day-1)/56+1}  /  พรุ่งนี้ {f.Forecast(f.Day+1)}",72,47,13,Muted);
        Panel(g,new(444,12,219,64));DrawIcon(g,9,453,22,29);if(f.Weather is "ฝนตก" or "พายุ"||f.Season==3)art.Draw(g,art.WeatherIcons[f.Weather=="พายุ"?1:f.Weather=="ฝนตก"?0:2],618,34,30,30);else DrawIcon(g,10,618,34,30);TextAt(g,$"{(int)f.Minute/60:00}:{(int)f.Minute%60:00}",489,17,23,Moss,true);
        TextAt(g,f.Weather,490,48,14,Muted);
        Panel(g,new(675,12,303,64));DrawIcon(g,7,685,20,26);TextAt(g,f.Coins.ToString("N0"),721,20,19,Walnut,true);DrawIcon(g,8,811,20,26);TextAt(g,$"{Me?.Energy??0}",846,20,19,Moss,true);
        Round(g,Color.FromArgb(223,212,176),new(812,53,142,7),3);int energy=Me?.Energy??0;if(energy>0)Round(g,Color.FromArgb(119,150,95),new(812,53,142*energy/100f,7),3);
        int active=0;long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();foreach(var p in f.Players)if(now-p.LastSeen<30000)active++;
        TextAt(g,$"{active}/4 ชาวสวน",691,49,12,Muted);
        UiButton(g,"เพลง",new(990,13,82,36),Music);UiButton(g,"เชิญ",new(1081,13,82,36),ShowInvite,disabled:invite=="");UiButton(g,"เมนู",new(1172,13,92,36),()=>OpenPanel("pause"));
        TextAt(g,mode,995,55,12,Muted);
    }
    void DrawHotbar(Graphics g) {
        Panel(g,new(264,703,752,75));
        for(int i=0;i<Tools.Length;i++){int index=i;var r=new RectangleF(279+i*66,712,58,57);bool active=tool==Tools[i].Key;
            Round(g,active?Gold:Color.FromArgb(221,213,182),r,4);Round(g,active?Color.FromArgb(249,224,163):Paper,new(r.X+2,r.Y+2,r.Width-4,r.Height-4),3);
            if(active){Line(g,Walnut,2,r.X+3,r.Bottom-2,r.Right-3,r.Bottom-2);}
            DrawIcon(g,i,r.X+13,r.Y+12,32);TextAt(g,(i+1).ToString(),r.X+4,r.Y+1,11,Walnut,true);
            buttons.Add((r,()=>tool=Tools[index].Key));
        }
        DrawIcon(g,11,754,721,30);UiButton(g,"กระเป๋า B",new(790,717,98,43),()=>OpenPanel("bag"));UiButton(g,"ร้านค้า O",new(897,717,103,43),()=>OpenPanel("market"));
        var crop=Farm.Crops[seed];Panel(g,new(16,703,234,75));art.Draw(g,art.Crops[art.CropIndex(seed),3],26,716,37,46);TextAt(g,tool=="plant"?crop.Name:ToolNames[Array.FindIndex(Tools,t=>t.Key==tool)],69,713,17,Moss,true);
        UiButton(g,"เลือกเมล็ด · Tab",new(69,742,166,26),()=>OpenPanel("seeds"));
        UiButton(g,"พักผ่อน",new(1030,707,106,31),()=>Queue(Me?.Sleeping==true?"wake":"sleep"));UiButton(g,"ขาย · E",new(1144,707,120,31),()=>Queue("sell"),icon:7);
        UiButton(g,"ภาพ · F3",new(1030,745,106,30),()=>OpenPanel("settings"));UiButton(g,"คู่มือ · F1",new(1144,745,120,30),Help);
    }
    void DrawFooter(Graphics g,Farm f) {
        string message=status!=""?status:f.Paused?"หยุดเวลาอยู่ · เจ้าของกด Esc เพื่อเล่นต่อ":f.Journal.FirstOrDefault()??"";
        TextAt(g,message,24,782,12,status!=""?Color.FromArgb(172,83,65):Walnut);
        if(f.Paused){Panel(g,new(514,100,252,37));TextAt(g,"เวลาหยุดอยู่ · Esc เล่นต่อ",535,108,15,Walnut,true);}
    }
    void DrawUiPanel(Graphics g) {
        buttons.Clear(); // The overlay exclusively owns input; covered hotbar buttons cannot fire.
        Box(g,Color.FromArgb(92,40,49,38),0,0,1280,800);
        var r=new RectangleF(278,116,724,538);Panel(g,r);
        string title=uiPanel switch {"seeds"=>"เมล็ดพันธุ์และฤดูกาล","bag"=>"กระเป๋าชาวสวน","market"=>"ร้านค้าประจำฟาร์ม","settings"=>"ภาพและเสียง","help"=>"คู่มือชาวสวน",_=>"พักที่ฟาร์ม"};
        TextAt(g,title,307,137,27,Moss,true);UiButton(g,"ปิด",new(918,137,57,34),()=>{uiPanel="";keys.Clear();});Line(g,Color.FromArgb(204,184,142),1,303,185,976,185);
        switch(uiPanel){case "seeds":DrawSeedShop(g);break;case "bag":DrawBag(g);break;case "market":DrawMarket(g);break;case "settings":DrawSettings(g);break;case "help":DrawHelpPanel(g);break;default:DrawPausePanel(g);break;}
    }
    void DrawSeedShop(Graphics g) {
        if(world==null)return;int i=0;
        foreach(var pair in Farm.Crops){string key=pair.Key;var crop=pair.Value;var r=new RectangleF(304+i%2*340,199+i/2*84,326,76);bool allowed=world.Greenhouse||crop.Seasons.Contains(world.Season);
            Round(g,key==seed?Color.FromArgb(218,193,140):Color.FromArgb(229,220,190),r,4);art.Draw(g,art.Crops[art.CropIndex(key),3],r.X+10,r.Y+8,44,55);
            TextAt(g,crop.Name,r.X+63,r.Y+8,17,allowed?Moss:Muted,true);TextAt(g,$"{crop.Cost} เหรียญ · โต {crop.Days} วัน · เก็บ {crop.Window} วัน",r.X+63,r.Y+34,12,Muted);
            TextAt(g,allowed?$"ราคาวันนี้ {world.SalePrice(key)} / ชิ้น":"ปลูกไม่ได้ในฤดูนี้",r.X+63,r.Y+53,12,allowed?Walnut:Color.FromArgb(170,103,78));
            if(allowed)buttons.Add((r,()=>{seed=key;tool="plant";uiPanel="";}));i++;
        }
        var selected=Farm.Crops[seed];TextAt(g,!world.Greenhouse&&selected.Days>14-world.SeasonDay?"ระวัง: เมล็ดที่เลือกอาจโตไม่ทันก่อนเปลี่ยนฤดู":"คลิกเลือกเมล็ด แล้วเดินใกล้แปลงเพื่อปลูก",307,558,16,Walnut,true);
        TextAt(g,"พืชผิดฤดูจะตายเมื่อเปลี่ยนฤดู · หลังคาเรือนกระจกช่วยป้องกัน",307,595,14,Muted);
    }
    void DrawBag(Graphics g) {
        if(world==null)return;int i=0,total=0;
        foreach(var pair in Farm.Crops){var r=new RectangleF(306+i%4*169,205+i/4*135,156,122);Round(g,Color.FromArgb(229,220,190),r,5);art.Draw(g,art.Crops[i,3],r.X+57,r.Y+10,43,54);int quantity=Me?.Bag.GetValueOrDefault(pair.Key)??0;total+=quantity*world.SalePrice(pair.Key);TextAt(g,pair.Value.Name,r.X+10,r.Y+69,14,Moss,true);TextAt(g,$"{quantity} ชิ้น  ·  {world.SalePrice(pair.Key)}/ชิ้น",r.X+10,r.Y+95,12,Muted);i++;}
        DrawIcon(g,7,308,495,35);TextAt(g,$"มูลค่าประมาณวันนี้: {total:N0} เหรียญ",354,501,21,Walnut,true);
        TextAt(g,"ผลผลิตเป็นของผู้เล่นแต่ละคน · ขายแล้วเข้าเงินกองกลาง\nผักในกระเป๋าลด 25% เมื่อข้ามคืน ควรนำไปขายให้ทัน",308,544,15,Muted,false,665);
        UiButton(g,"ขายที่ลัง · E",new(757,595,218,36),()=>Queue("sell"),icon:7);
    }
    void DrawMarket(Graphics g) {
        if(world==null)return;
        art.Draw(g,art.Houses[1],306,214,168,157);TextAt(g,"เรือนกระจกให้สวนเติบโตได้ทุกฤดู",497,211,20,Moss,true);
        TextAt(g,"หลังคา 300 เหรียญ\nต้องอยู่ใกล้ร้านค้า และเจ้าของฟาร์มเป็นผู้ซื้อ\nใช้งบกองกลาง ทุกคนได้ใช้ร่วมกัน",497,254,15,Muted,false,477);
        UiButton(g,world.Greenhouse?"ติดตั้งหลังคาแล้ว":"ซื้อหลังคาเรือนกระจก",new(497,342,475,44),()=>Queue("greenhouse"),disabled:world.Greenhouse||Me?.Id!=world.Owner,icon:0);
        Line(g,Color.FromArgb(204,184,142),1,308,412,972,412);TextAt(g,"ทุนฉุกเฉิน",308,433,21,Moss,true);TextAt(g,"เมื่อเงินกองกลางไม่พอเริ่มปลูก สามารถขอทุนได้ตามเงื่อนไขเดิม",308,473,15,Muted);
        UiButton(g,"ขอทุนฉุกเฉิน",new(308,517,235,42),()=>Queue("rescue"),icon:7);UiButton(g,"เลือกเมล็ดพันธุ์",new(554,517,238,42),()=>uiPanel="seeds",icon:0);
        TextAt(g,"ตลาดใช้ราคาของวันนี้ · เปิดกระเป๋าเพื่อดูราคาผลผลิตแต่ละชนิด",308,593,14,Muted);
    }
    void DrawSettings(Graphics g) {
        TextAt(g,"คุณภาพภาพ",309,214,20,Moss,true);
        string[] labels={"ต่ำ","กลาง","สูง"};for(int i=0;i<3;i++){int level=i;UiButton(g,labels[i],new(310+i*219,257,207,44),()=>{quality=level;SaveVisualPreferences();},quality==i);}
        TextAt(g,"ต่ำ: ลดเงาและฝน  ·  กลาง: สมดุล  ·  สูง: แสง ละออง และบรรยากาศครบ",310,322,14,Muted);
        UiButton(g,reducedMotion?"ลดการเคลื่อนไหว: เปิด":"ลดการเคลื่อนไหว: ปิด",new(310,365,320,43),()=>{reducedMotion=!reducedMotion;SaveVisualPreferences();},reducedMotion);
        UiButton(g,soundEnabled?"เสียงการกระทำ: เปิด":"เสียงการกระทำ: ปิด",new(645,365,320,43),()=>{soundEnabled=!soundEnabled;SaveVisualPreferences();},soundEnabled);
        TextAt(g,"ระยะกล้อง",310,440,19,Moss,true);
        UiButton(g,"มองกว้าง",new(310,479,208,42),()=>{zoom=Math.Max(1,zoom-.15f);cameraReady=false;SaveVisualPreferences();});UiButton(g,$"{zoom:0.00} ×",new(531,479,208,42),()=>{zoom=1.35f;cameraReady=false;SaveVisualPreferences();});UiButton(g,"มองใกล้",new(752,479,208,42),()=>{zoom=Math.Min(1.7f,zoom+.15f);cameraReady=false;SaveVisualPreferences();});
        TextAt(g,"F2 ลดการเคลื่อนไหว · M เปิด/ปิดเสียง · + / − ปรับระยะกล้อง\nการตั้งค่าภาพบันทึกแยกจากเซฟฟาร์ม",310,562,15,Muted,false,650);
    }
    void DrawHelpPanel(Graphics g) {
        TextAt(g,"WASD / ลูกศร เดิน    คลิกแปลงใกล้ตัว ใช้เครื่องมือ\n1–7 เลือกเครื่องมือ    Tab เมล็ด    B กระเป๋า    O ร้านค้า\nE ขายที่ลัง    Space นอนหน้าบ้าน    Esc เจ้าของหยุดเวลา\nF2 ลดการเคลื่อนไหว    F3 ภาพและเสียง    M เปิด/ปิดเสียง",310,206,17,Moss,false,660);
        TextAt(g,"ดูแลสวนให้ทันฤดู",310,325,22,Moss,true);
        TextAt(g,"ฤดูละ 14 วัน · หนึ่งวันประมาณ 10 นาที · ทุกคนเข้านอนเพื่อข้ามวัน\nรดน้ำทุกวัน ดินแห้งหยุดโต ฝนมากเสี่ยงโรคและรากเน่า\nไม้ค้ำกันพายุ ปุ๋ยฟื้นดิน ปลูกซ้ำทำให้ดินเสื่อม\nเก็บเกี่ยวให้ทันใน 1–3 วัน ผักในกระเป๋าลด 25% ทุกเช้า\nซื้อหลังคาที่ร้านเพื่อปลูกข้ามฤดู · ใช้เงินกองกลางร่วมกับเพื่อน",310,368,16,Muted,false,664);
        TextAt(g,"ออนไลน์: ส่งรหัสเชิญเต็มให้เพื่อน และเจ้าของต้องเปิดเกมไว้\nเซฟอัตโนมัติใน LocalAppData\\SuanPleng · เวลาหยุดเมื่อปิดเกม\nเพลง YouTube เปิดในเบราว์เซอร์แยกจากตัวเกม",310,529,14,Muted,false,664);
    }
    void DrawPausePanel(Graphics g) {
        if(world==null)return;TextAt(g,"พักสายตา แล้วกลับไปดูแลสวน",309,211,20,Moss,true);
        UiButton(g,world.Paused?"เล่นต่อ / เดินเวลา":"หยุดเวลา · เจ้าของฟาร์ม",new(310,266,656,46),()=>Queue("pause"),disabled:Me?.Id!=world.Owner,icon:9);
        UiButton(g,"กระเป๋า",new(310,331,320,43),()=>uiPanel="bag",icon:11);UiButton(g,"ภาพและเสียง",new(646,331,320,43),()=>uiPanel="settings",icon:10);
        UiButton(g,"ฟังเพลง YouTube",new(310,390,320,43),Music);UiButton(g,"คัดลอกรหัสเชิญ",new(646,390,320,43),ShowInvite,disabled:invite=="");
        UiButton(g,"กลับไปฟาร์ม",new(310,449,656,43),()=>uiPanel="");UiButton(g,"ออกจากฟาร์ม / บันทึก",new(310,526,656,43),()=>{if(MessageBox.Show("ออกจากฟาร์ม? หากเป็นเจ้าของ ห้องเพื่อนจะปิดด้วย","สวนเพลง",MessageBoxButtons.YesNo)==DialogResult.Yes){uiPanel="";LeaveFarm();}});
        TextAt(g,"เวลาจะหยุดเมื่อเจ้าของกดหยุดเวลา · เปิดเมนูอย่างเดียวเวลาเดินต่อ",310,602,14,Muted);
    }
    void DrawPlotInspection(Graphics g,Farm f) {
        if(renderPreview)return;Point cursor=PointToClient(Cursor.Position);var mouse=new PointF((cursor.X-ox)/scale,(cursor.Y-oy)/scale);if(!SceneView.Contains(mouse))return;
        var worldPoint=SceneToWorld(mouse);
        for(int i=0;i<24;i++){var t=TilePoint(i);if(!new RectangleF(t.X,t.Y,35,35).Contains(worldPoint))continue;
            var screen=WorldToScene(t);using(var pen=new Pen(Color.FromArgb(211,242,223,158),2))g.DrawRectangle(pen,screen.X,screen.Y,35*zoom,35*zoom);
            var p=f.Plots[i];var r=new RectangleF(Math.Clamp(mouse.X+16,25,893),Math.Clamp(mouse.Y+14,94,580),355,101);Panel(g,r);
            string title=p.Crop==""?$"แปลง {i+1} · ว่าง":Farm.Crops[p.Crop].Name;TextAt(g,title,r.X+12,r.Y+8,17,Moss,true);TextAt(g,$"น้ำ {p.Moisture}   ดิน {p.Fertility}   คุณภาพ {p.Quality}",r.X+12,r.Y+37,14,Muted);
            string detail=p.Crop==""?"แปลงพักฟื้นดินวันละ 3":p.RipeDay>=0?$"พร้อมเก็บ · เหลือ {Farm.Crops[p.Crop].Window-(f.Day-p.RipeDay)} วัน":p.Disease?"ป่วย! รักษาก่อนโรคลาม":$"โต {p.Growth:0.#}/{Farm.Crops[p.Crop].Days} วัน · รดน้ำทุกวัน";
            TextAt(g,detail,r.X+12,r.Y+68,14,Walnut,true);break;
        }
    }
}
