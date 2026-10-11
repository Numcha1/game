using Godot;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;

public partial class FarmHud : Control
{
    public static readonly (string Action, string Label, string Icon)[] Tools = {
        ("plant", "เมล็ด", "seeds"), ("water", "รดน้ำ", "water"), ("harvest", "เก็บเกี่ยว", "carrot"),
        ("treat", "รักษา", "flowers"), ("fertilize", "ปุ๋ย", "seeds"), ("stake", "ไม้ค้ำ", "hoe"), ("clear", "ถอนต้น", "hoe") };
    public bool ModalVisible => modal?.Visible == true;
    FarmScene farm = null!;
    Label status = null!, seed = null!, toast = null!, coins = null!;
    ProgressBar energy = null!;
    Label plotInfo = null!;
    PanelContainer inspector = null!;
    public string PlotInformation => plotInfo.Text;
    public bool PlotInspectorVisible => inspector.Visible;
    VBoxContainer modalContent = null!;
    Control modal = null!;
    PanelContainer guide = null!;
    public bool TutorialVisible=>guide.Visible;
    public void ShowTutorial()=>guide.Visible=true;
    Button[] tools = Array.Empty<Button>();
    double toastTime, refreshElapsed;
    bool dirty;
    int currentMenu, menuDay;
    bool pauseMenuActive;
    Control toolPanel=null!;
    Button shopButton=null!;
    void OnStateChanged(GameChange change)
    {
        if (change != GameChange.Position) dirty = true;
        if (change == GameChange.Position && farm.SelectedPlot >= 0) farm.InspectPlot(-1);
        if ((change & GameChange.Plots) != 0) RefreshInspector();
        if(ModalVisible && currentMenu>0 && (change & GameChange.Inventory)!=0) {
            if(currentMenu==1)OpenBag();else if(currentMenu==3)OpenStorage();else if(currentMenu==2 && menuDay!=farm.Session.Read.World.Day)OpenShop();
        }
    }
    string lastStatus = "", lastCoins = "", lastSeed = "";
    Button Button(string text, Action action, float minimumWidth = 48)
    {
        var button = new Button { Text = text, TooltipText = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, FocusMode = FocusModeEnum.All, CustomMinimumSize = new Vector2(minimumWidth, 30) };
        button.Pressed += () => { farm.PlayUiClick(); action(); }; return button;
    }
    public void Initialize(FarmScene scene)
    {
        if (farm != null) throw new InvalidOperationException("HUD already initialized");
        farm = scene;
        Theme = FarmTypography.Shared;
        var top = new PanelContainer { Position = new Vector2(14, 12), Size = new Vector2(280,54), CustomMinimumSize = new Vector2(280, 54) }; top.AddThemeStyleboxOverride("panel", Theme.GetStylebox("panel","PanelContainer")); AddChild(top);
        var topRows = new VBoxContainer(); top.AddChild(topRows); status = new Label { AutowrapMode=TextServer.AutowrapMode.WordSmart }; topRows.AddChild(status);
        var stats = new HBoxContainer(); topRows.AddChild(stats); coins = new Label { CustomMinimumSize = new Vector2(120, 22) }; stats.AddChild(coins);
        energy = new ProgressBar { CustomMinimumSize = new Vector2(90, 15), MaxValue = 100, ShowPercentage = false }; stats.AddChild(energy);
        energy.AddThemeStyleboxOverride("background", Theme.GetStylebox("background","ProgressBar")); energy.AddThemeStyleboxOverride("fill", Theme.GetStylebox("fill","ProgressBar"));
        inspector = new PanelContainer { Position = new Vector2(14, 84), CustomMinimumSize = new Vector2(218, 126), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        inspector.AddThemeStyleboxOverride("panel", Theme.GetStylebox("panel","PanelContainer")); AddChild(inspector);
        plotInfo = new Label { CustomMinimumSize = new Vector2(190, 110), AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = MouseFilterEnum.Ignore };
        plotInfo.ThemeTypeVariation = "Compact"; inspector.AddChild(plotInfo);
        top.Resized += () => inspector.Position = new Vector2(14,top.GetRect().End.Y+8);
        var topRight = new HBoxContainer { AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -310, OffsetRight = -14, OffsetTop = 12, OffsetBottom = 44 }; AddChild(topRight);
        topRight.AddChild(Button("กระเป๋า B", OpenBag,110)); shopButton=Button("ร้านค้า O", OpenShop,110);topRight.AddChild(shopButton); topRight.AddChild(Button("☰", OpenSettings));
        var bottom = new PanelContainer { AnchorLeft = .5f, AnchorRight = .5f, AnchorTop = 1, AnchorBottom = 1, OffsetLeft = -286, OffsetRight = 286, OffsetTop = -82, OffsetBottom = -12, GrowVertical = GrowDirection.Begin };
        bottom.AddThemeStyleboxOverride("panel", Theme.GetStylebox("panel","ToolPanel")); AddChild(bottom);
        toolPanel=bottom;
        var rows = new VBoxContainer(); bottom.AddChild(rows); seed = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center }; rows.AddChild(seed);
        var bar = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }; rows.AddChild(bar); tools = new Button[Tools.Length];
        for (int i = 0; i < Tools.Length; i++)
        {
            int index = i; var b = Button((i + 1).ToString(), () => { farm.Tool = Tools[index].Action; Refresh(); });
            b.FocusMode = FocusModeEnum.None; b.TooltipText = Tools[i].Label + " · " + (i + 1); b.Icon = farm.Art.ToolIcon(Tools[i].Action); b.ExpandIcon = true; b.AddThemeConstantOverride("icon_max_width", 24); b.CustomMinimumSize = new Vector2(67, 32);
            bar.AddChild(b); tools[i] = b;
        }
        toast = new Label { AnchorLeft = .5f, AnchorRight = .5f, AnchorTop = 1, AnchorBottom = 1, OffsetLeft = -360, OffsetRight = 360, OffsetTop = -134, OffsetBottom = -91,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, Visible = false };
        toast.AddThemeColorOverride("font_color", new Color("fff2cc")); toast.AddThemeColorOverride("font_shadow_color", new Color("2e3f32")); toast.AddThemeConstantOverride("shadow_offset_x", 1); toast.AddThemeConstantOverride("shadow_offset_y", 2); AddChild(toast);
        guide=new PanelContainer { AnchorLeft=1,AnchorRight=1,OffsetLeft=-270,OffsetRight=-14,OffsetTop=58,Visible=false };AddChild(guide);
        var guideRows=new VBoxContainer();guide.AddChild(guideRows);
        guideRows.AddChild(new Label { Text="เริ่มฟาร์มของคุณ",ThemeTypeVariation="ItemHeading",HorizontalAlignment=HorizontalAlignment.Center });
        guideRows.AddChild(new Label { Text="1 เมล็ด → คลิกแปลงใกล้ตัว\n2 รดน้ำ → 3 เก็บเกี่ยว\nE ขายใกล้ลัง · Space นอนหน้าบ้าน",AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new Vector2(240,0) });
        guideRows.AddChild(Button("เข้าใจแล้ว",()=>{guide.Visible=false;GetViewport().GuiGetFocusOwner()?.ReleaseFocus();}));
        modal = new Control { AnchorRight = 1, AnchorBottom = 1, Visible = false, MouseFilter = MouseFilterEnum.Stop }; AddChild(modal);
        var dim = new ColorRect { AnchorRight = 1, AnchorBottom = 1, Color = new Color(0.08f, .13f, .1f, .65f) }; modal.AddChild(dim);
        var center = new CenterContainer { AnchorRight = 1, AnchorBottom = 1 }; modal.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(500, 350) }; panel.AddThemeStyleboxOverride("panel", Theme.GetStylebox("panel","PanelContainer")); center.AddChild(panel);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(470, 340) }; panel.AddChild(scroll);
        modalContent = new VBoxContainer { CustomMinimumSize = new Vector2(465, 320) }; scroll.AddChild(modalContent);
        scene.Message += OnMessage;
        scene.Session.StateChanged += OnStateChanged; Refresh();
    }
    void OnMessage(string message) { toast.Text = message; toast.Visible = true; toastTime = 5; }
    public override void _ExitTree()
    {
        if (farm != null && GodotObject.IsInstanceValid(farm)) { farm.Message -= OnMessage; farm.Session.StateChanged -= OnStateChanged; }
    }
    public void SetTextScale(int index) { Theme = FarmTypography.AtScale(index); }
    public void SetUiScale(int index) {
        float zoom=1+Math.Clamp(index,0,2)*.1f;
        SetAnchorsPreset(LayoutPreset.TopLeft);Position=Vector2.Zero;Size=GetViewport().GetVisibleRect().Size/zoom;Scale=Vector2.One*zoom;
    }
    public void Refresh()
    {
        toolPanel.Visible=!farm.Session.InsideBuilding;shopButton.Disabled=farm.Session.InsideBuilding;
        if(farm.Session.InsideBuilding)guide.Visible=false;
        var state = farm.Session.Read.World;
        string s = $"{GameCatalog.SeasonNames[state.Season]} · วันที่ {state.SeasonDay} · {(int)state.Minute / 60:00}:{(int)state.Minute % 60:00} · {state.Weather}";
        if (s != lastStatus) { status.Text = s; lastStatus = s; }
        string c = $"● {state.Coins:N0} เหรียญ   แรง"; if (c != lastCoins) { coins.Text = c; lastCoins = c; }
        energy.Value = farm.Session.Read.Player.Energy;
        string chosen = $"{GameCatalog.Crops[farm.Seed].Name} · Tab เปลี่ยนเมล็ด · F1 วิธีเล่น · {Tools.First(t => t.Action == farm.Tool).Label}";
        if (chosen != lastSeed) { seed.Text = chosen; lastSeed = chosen; }
        for (int i = 0; i < tools.Length; i++) tools[i].Modulate = Tools[i].Action == farm.Tool ? new Color("ffd484") : Colors.White;
        RefreshInspector();
    }
    void RefreshInspector()
    {
        var read = farm.Session.Read;
        string next = "พรุ่งนี้ · " + read.TomorrowWeather;
        inspector.Visible = farm.SelectedPlot >= 0;
        if (farm.SelectedPlot < 0) {
            plotInfo.Text = "เริ่มฟาร์มของคุณ\n1 เลือกเมล็ด · คลิกแปลงใกล้ตัว\n2 รดน้ำ · 3 เก็บเกี่ยวเมื่อโต\nE ขายที่ลัง · Space นอนหน้าบ้าน\n" + next; return;
        }
        var plot = read.Plot(farm.SelectedPlot);
        string name = plot.Crop == "" ? "แปลงว่าง" : GameCatalog.Crops[plot.Crop].Name;
        string growth = plot.Crop == "" ? "เลือกเมล็ดแล้วคลิกเพื่อปลูก" : plot.RipeDay >= 0
            ? $"เก็บได้แล้ว · วันสุดท้าย {plot.RipeDay + GameCatalog.Crops[plot.Crop].HarvestWindow} (วันฟาร์ม)"
            : $"เติบโต {plot.Growth:0.#}/{GameCatalog.Crops[plot.Crop].Days} วัน";
        string hint = plot.Disease ? "ป่วย · ใช้ 4 รักษา ก่อนลาม" : plot.Crop != "" ? "คุณภาพ " + plot.Quality + "% · " + (plot.Stake ? "มีไม้ค้ำ" : "ไม่มีไม้ค้ำ")
            : plot.LastCrop != "" ? "พืชก่อนหน้า · " + GameCatalog.Crops.GetValueOrDefault(plot.LastCrop)?.Name : "ปลูกพืชสลับชนิดเพื่อรักษาดิน";
        plotInfo.Text = $"แปลง {farm.SelectedPlot + 1} · {name}\n{growth}\nน้ำ {plot.Moisture}% · ดิน {plot.Fertility}%\n{hint}\n{next}";
    }
    public void RequestPlotAction(string action, int plot)
    {
        if (action != "clear" || farm.Session.Read.Plot(plot).Crop == "") { farm.Perform(action, plot); return; }
        Open("ยืนยันถอนต้น");
        modalContent.AddChild(new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, Text = "ต้นนี้จะถูกถอนโดยไม่ได้ผลผลิต และใช้แรงตามกฎเดิม" });
        modalContent.AddChild(Button("ยืนยันถอนต้น", () => { CloseModal(); farm.Perform(action, plot); }));
    }
    public void RequestSleep()
    {
        if (farm.Session.Read.Player.Sleeping) { farm.Perform("wake"); return; }
        if (farm.Session.Read.Inventory().Count == 0) { farm.Perform("sleep"); return; }
        Open("เข้านอนพร้อมผลผลิต?");
        modalContent.AddChild(new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, Text = "เมื่อเริ่มวันใหม่ ผลผลิตในกระเป๋าลด 25%\nขายที่ลังก่อนนอนเพื่อรับเงินเต็มจำนวน" });
        var confirm=Button("ยืนยันเข้านอน", () => { CloseModal(); farm.Perform("sleep"); });confirm.Name="ConfirmSleep";modalContent.AddChild(confirm);
    }
    void Open(string title)
    {
        currentMenu=0;
        foreach (Node child in modalContent.GetChildren()) { modalContent.RemoveChild(child); child.QueueFree(); }
        modalContent.AddChild(new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, Text = title, ThemeTypeVariation = "Heading", HorizontalAlignment = HorizontalAlignment.Center });
        modalContent.AddChild(Button("ปิด · Esc", CloseModal)); modal.Visible = true; farm.Farmer.InputBlocked = true;
        modalContent.GetChild<Button>(1).GrabFocus();
    }
    public void CloseModal()
    {
        if (farm.TitleActive) { OpenTitle(); return; }
        if (pauseMenuActive) { pauseMenuActive=false; if(farm.Session.Read.World.Paused)farm.Perform("pause"); }
        GetViewport().GuiGetFocusOwner()?.ReleaseFocus(); modal.Visible = false; farm.Farmer.InputBlocked = farm.IsBusy;
    }
    public void OpenTitle()
    {
        farm.ShowTitle(); Open("Seasons Together");
        var close=modalContent.GetChild<Button>(1);modalContent.RemoveChild(close);close.QueueFree();
        modalContent.AddChild(new Label { Text="สวนเพลง · ฟาร์มของคุณในทุกฤดู",AutowrapMode=TextServer.AutowrapMode.WordSmart,HorizontalAlignment=HorizontalAlignment.Center });
        var play=Button("เข้าฟาร์ม",()=>{farm.StartPlaying();CloseModal();if(farm.Session.Read.World.Day==1)ShowTutorial();});modalContent.AddChild(play);
        modalContent.AddChild(Button("ตั้งค่า",OpenSettings));modalContent.AddChild(Button("วิธีเล่น",OpenHelp));
        modalContent.AddChild(Button("บันทึกและออก",farm.RequestQuit));
        modalContent.AddChild(new Label { Text="โหมดคนเดียว · ใช้ฟาร์มที่บันทึกไว้โดยอัตโนมัติ",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        play.GrabFocus();
    }
    public void OpenPause()
    {
        if(!farm.Session.Read.World.Paused)farm.Perform("pause");
        if(!farm.Session.Read.World.Paused)return;
        pauseMenuActive=true;Open("พักฟาร์ม");
        modalContent.GetChild<Button>(1).Text="กลับเข้าฟาร์ม · Esc";
        modalContent.AddChild(Button("ตั้งค่า",OpenSettings));modalContent.AddChild(Button("วิธีเล่น",OpenHelp));
        modalContent.AddChild(Button("บันทึกฟาร์ม",farm.SaveNow));modalContent.AddChild(Button("บันทึกและออก",farm.RequestQuit));
    }
    public void OpenBag()
    {
        Open("กระเป๋าผลผลิต");
        var inventory = farm.Session.Read.Inventory();
        if (inventory.Count == 0) modalContent.AddChild(new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, Text = "ยังไม่มีผลผลิตในกระเป๋า" });
        var grid=new GridContainer { Columns=2,SizeFlagsHorizontal=SizeFlags.ExpandFill };modalContent.AddChild(grid);
        foreach (var pair in inventory) AddItemCard(grid,pair.Crop,pair.Name,$"{pair.Quantity} ชิ้น\nราคาวันนี้ {pair.UnitPrice} เหรียญ",()=>OpenItem(pair));
        modalContent.AddChild(Button("ตรวจรายได้และขายทั้งหมด · ใกล้ลังขาย", RequestSale));
        modalContent.AddChild(new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, Text = "ผลผลิตลด 25% เมื่อข้ามคืน ควรขายก่อนเข้านอน" });
        currentMenu=1;menuDay=farm.Session.Read.World.Day;
    }
    public string SalePreviewText { get; private set; } = "";
    public void RequestSale()
    {
        var quote = farm.Session.Read.QuoteSale();
        if (quote.Items.Count == 0) { if (ModalVisible) CloseModal(); farm.Notify("ยังไม่มีผลผลิตในกระเป๋า"); return; }
        Open("ตรวจรายได้ก่อนขาย");
        SalePreviewText = string.Join("\n", quote.Items.Select(item => $"{item.Name} × {item.Quantity} · {item.UnitPrice} เหรียญ/ชิ้น"))
            + $"\nรวม {quote.Total:N0} เหรียญ · ราคาวันนี้\nต้องยืนใกล้ลังขายด้านขวา ราคาจะตรวจอีกครั้งเมื่อยืนยัน";
        modalContent.AddChild(new Label { Text = SalePreviewText, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        bool submitted = false;
        var confirm = Button("ยืนยันขายทั้งหมด", () => {
            if (submitted) return;
            if (!quote.Matches(farm.Session.Read.QuoteSale())) {
                RequestSale(); farm.Notify("วันหรือผลผลิตเปลี่ยนแล้ว กรุณาตรวจรายได้ใหม่"); return;
            }
            submitted = true; CloseModal(); farm.Perform("sell");
        });
        confirm.Name = "ConfirmSale"; modalContent.AddChild(confirm);
    }
    void AddItemCard(GridContainer grid,string key,string title,string description,Action action,bool selected=false) {
        var card=farm.Art.ItemCardScene.Instantiate<FarmItemCard>();card.Name="Item-"+key;grid.AddChild(card);
        card.Configure(title,description,farm.Art.CropStage(key,3),()=>{farm.PlayUiClick();action();},selected);
    }
    void OpenItem(InventoryItem item) {
        Open(item.Name);modalContent.AddChild(new Label { Text=$"มี {item.Quantity} ชิ้น · ราคาที่แสดงสำหรับวันนี้ {item.UnitPrice} เหรียญ\nขายใกล้ลังเพื่อรับราคาจริง ณ เวลาขาย",AutowrapMode=TextServer.AutowrapMode.WordSmart });
        modalContent.AddChild(Button("กลับกระเป๋า",OpenBag));
    }
    public void OpenShop()
    {
        Open("เมล็ดและอุปกรณ์");
        var grid=new GridContainer { Columns=2,SizeFlagsHorizontal=SizeFlags.ExpandFill };modalContent.AddChild(grid);
        foreach (var key in farm.CropKeys)
        {
            string cropKey = key; var crop = GameCatalog.Crops[key];
            string availability=farm.Session.Read.World.Greenhouse || crop.Seasons.Contains(farm.Session.Read.World.Season)?"ปลูกได้ฤดูนี้":"นอกฤดู";
            AddItemCard(grid,key,crop.Name,$"{crop.Cost} เหรียญ · {crop.Days} วัน\n{availability}",()=>{farm.Seed=cropKey;CloseModal();Refresh();},farm.Seed==key);
        }
        modalContent.AddChild(Button("หลังคาเรือนกระจก · 300 เหรียญ · ใกล้ร้านค้า", () => { CloseModal(); farm.Perform("greenhouse"); }));
        modalContent.AddChild(Button("ทุนฉุกเฉิน · เงินต่ำกว่า 6 เหรียญ", () => { CloseModal(); farm.Perform("rescue"); }));
        modalContent.AddChild(new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, Text = "เลือกเมล็ดที่นี่ แล้วปลูกในแปลงเพื่อจ่ายเงิน\nเรือนกระจกช่วยให้ปลูกได้ทุกฤดูและกันพายุ" });
        currentMenu=2;menuDay=farm.Session.Read.World.Day;
    }
    public void OpenSettings()
    {
        Open("ตั้งค่าภาพและเซฟ");
        if(farm.Session.InsideBuilding)modalContent.AddChild(Button("กลับฟาร์มอย่างปลอดภัย",()=>{CloseModal();farm.RequestBuildingExit(true);}));
        for (int i = 0; i < 3; i++) { int q = i; modalContent.AddChild(Button(new[] { "ต่ำ", "กลาง", "สูง" }[i], () => { farm.SaveQuality(q); farm.Notify("ตั้งค่าคุณภาพภาพแล้ว"); })); }
        modalContent.AddChild(new Label { Text = "หน้าจอและการเข้าถึง", ThemeTypeVariation = "Heading" });
        foreach (var option in new[] { (0, "หน้าต่าง"), (1, "เต็มจอ"), (2, "หน้าต่างไร้ขอบ") }) {
            var chosen = option; modalContent.AddChild(Button(chosen.Item2, () => farm.SetDisplayMode(chosen.Item1)));
        }
        foreach (var option in new[] { (0, "ตัวอักษรปกติ"), (1, "ตัวอักษรใหญ่"), (2, "ตัวอักษรใหญ่พิเศษ") }) {
            var chosen = option; modalContent.AddChild(Button(chosen.Item2, () => farm.SetTextScale(chosen.Item1)));
        }
        foreach(var option in new[]{(0,"ขนาดเมนู 100%"),(1,"ขนาดเมนู 110%"),(2,"ขนาดเมนู 120%")}) {
            var chosen=option;modalContent.AddChild(Button(chosen.Item2,()=>farm.SetUiScale(chosen.Item1)));
        }
        var motion = new CheckButton { Text = "ลดการเคลื่อนไหว", ButtonPressed = farm.Display.ReducedMotion };
        motion.Toggled += farm.SetReducedMotion; modalContent.AddChild(motion);
        modalContent.AddChild(new Label { Text="เสียงเอฟเฟกต์ · 0 ปิดเสียง" });
        var volume=new HSlider { MinValue=0,MaxValue=100,Step=5,Value=farm.Display.EffectsVolume,CustomMinimumSize=new Vector2(240,24) };
        volume.ValueChanged+=value=>farm.SetEffectsVolume((int)value);modalContent.AddChild(volume);
        modalContent.AddChild(Button("วิทยุ YouTube · เปิดในเบราว์เซอร์", OpenMusic));
        modalContent.AddChild(Button("วิธีเล่น", OpenHelp));
        modalContent.AddChild(Button("บันทึกเหตุการณ์ฟาร์ม", OpenJournal));
        modalContent.AddChild(Button("นำเข้าเซฟเกมเดิม (คัดลอก)", () => {
            var dialog = new ConfirmationDialog { DialogText = "แทนฟาร์ม Godot ด้วยสำเนาเซฟเดิม?\nเซฟเกมเดิมจะไม่ถูกแก้ไข และสำรองฟาร์ม Godot ปัจจุบันไว้ก่อน" };
            AddChild(dialog); dialog.Confirmed += () => { farm.ImportLegacySave(); dialog.QueueFree(); }; dialog.Canceled += () => dialog.QueueFree(); dialog.PopupCentered(); }));
        modalContent.AddChild(Button("บันทึกฟาร์ม", farm.SaveNow));
    }
    public void OpenHelp()
    {
        Open("สวนเพลง · วิธีเล่น");
        modalContent.AddChild(new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, Text = "WASD / ลูกศร เดิน · 1–7 เครื่องมือ · Tab เมล็ด\nคลิกแปลงใกล้ตัวเพื่อปลูก รดน้ำ เก็บเกี่ยว\nชี้แปลงเพื่อดูน้ำ ดิน โรค และวันเก็บเกี่ยว\nE ขายใกล้ลังด้านขวา · Space นอนหน้าบ้าน\nEsc หยุดเวลา · B กระเป๋า · O ร้าน · F3 ตั้งค่า\nฤดูละ 14 วัน แรงฟื้น 100 เมื่อเริ่มวันใหม่\nพืชขาดน้ำหยุดโต ดินอ่อนทำให้โตช้า\nพายุทำร้ายพืช ใช้ไม้ค้ำหรือเรือนกระจก\nระวังเก็บช้า พ้นฤดู และผลผลิตเสีย 25% ข้ามคืน\nฟาร์มฉากนี้เป็นโหมดคนเดียว ยังไม่เปิดห้องออนไลน์\nใช้โปรไฟล์ Godot แยกจากเกม Windows เดิม" });
    }
    public void OpenJournal()
    {
        Open("เหตุการณ์ล่าสุด");
        foreach (string message in farm.Session.Read.Journal()) modalContent.AddChild(new Label {
            Text = message, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(445, 24) });
    }
    void OpenMusic()
    {
        Open("วิทยุ YouTube");
        modalContent.AddChild(new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, Text = "วางลิงก์ HTTPS ของ YouTube เพื่อฟังในเบราว์เซอร์" });
        var input = new LineEdit { PlaceholderText = "https://www.youtube.com/watch?v=…", MaxLength = 512 }; modalContent.AddChild(input);
        modalContent.AddChild(Button("เปิดเพลง", () => {
            if (!MusicLink.TryParse(input.Text, out var uri)) { farm.Notify("ใส่ลิงก์ HTTPS ของ YouTube เท่านั้น"); return; }
            if (OS.ShellOpen(uri!.AbsoluteUri) != Error.Ok) farm.Notify("เปิดเบราว์เซอร์ไม่ได้"); else CloseModal();
        }));
    }
    public override void _Input(InputEvent ev)
    {
        if (!ModalVisible && farm != null && ev is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Tab }) { FarmInputRouter.Handle(farm, ev); GetViewport().SetInputAsHandled(); return; }
        if (ModalVisible && ev is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape }) { CloseModal(); GetViewport().SetInputAsHandled(); }
    }
    public override void _Process(double delta) { refreshElapsed += delta; if (dirty && refreshElapsed >= .25) { dirty = false; refreshElapsed = 0; Refresh(); } if (toastTime > 0) { toastTime -= delta; if (toastTime <= 0) toast.Visible = false; } }
}
