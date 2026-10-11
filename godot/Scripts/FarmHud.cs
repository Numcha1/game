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
    VBoxContainer modalContent = null!;
    Control modal = null!;
    Button[] tools = Array.Empty<Button>();
    double toastTime, refreshElapsed;
    bool dirty;
    void OnStateChanged(GameChange change) { if (change != GameChange.Position) dirty = true; }
    string lastStatus = "", lastCoins = "", lastSeed = "";
    StyleBoxFlat PanelStyle(string color, string border = "69553b") => new() {
        BgColor = new Color(color), BorderColor = new Color(border), BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
        CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
        ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 7, ContentMarginBottom = 7,
        ShadowColor = new Color(0, 0, 0, .22f), ShadowSize = 4, ShadowOffset = new Vector2(0, 3) };
    Button Button(string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(48, 30) };
        button.Pressed += action; return button;
    }
    public void Initialize(FarmScene scene)
    {
        if (farm != null) throw new InvalidOperationException("HUD already initialized");
        farm = scene;
        var theme = new Theme { DefaultFont = new SystemFont { FontNames = new string[] { "Tahoma", "Noto Sans Thai", "sans-serif" } }, DefaultFontSize = 14 };
        theme.SetColor("font_color", "Label", new Color("443d2f"));
        theme.SetColor("font_color", "Button", new Color("443d2f")); theme.SetColor("font_hover_color", "Button", new Color("443d2f"));
        theme.SetStylebox("normal", "Button", PanelStyle("eee0b8")); theme.SetStylebox("hover", "Button", PanelStyle("f8eccd"));
        theme.SetStylebox("pressed", "Button", PanelStyle("ccaa62")); Theme = theme;
        var top = new PanelContainer { Position = new Vector2(14, 12), CustomMinimumSize = new Vector2(310, 54) }; top.AddThemeStyleboxOverride("panel", PanelStyle("f3e5c7")); AddChild(top);
        var topRows = new VBoxContainer(); top.AddChild(topRows); status = new Label(); topRows.AddChild(status);
        var stats = new HBoxContainer(); topRows.AddChild(stats); coins = new Label { CustomMinimumSize = new Vector2(120, 22) }; stats.AddChild(coins);
        energy = new ProgressBar { CustomMinimumSize = new Vector2(135, 15), MaxValue = 100, ShowPercentage = false }; stats.AddChild(energy);
        energy.AddThemeStyleboxOverride("background", PanelStyle("bba985")); energy.AddThemeStyleboxOverride("fill", PanelStyle("729552", "729552"));
        var topRight = new HBoxContainer { AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -246, OffsetRight = -14, OffsetTop = 12, OffsetBottom = 44 }; AddChild(topRight);
        topRight.AddChild(Button("กระเป๋า B", OpenBag)); topRight.AddChild(Button("ร้านค้า O", OpenShop)); topRight.AddChild(Button("☰", OpenSettings));
        var bottom = new PanelContainer { AnchorLeft = .5f, AnchorRight = .5f, AnchorTop = 1, AnchorBottom = 1, OffsetLeft = -286, OffsetRight = 286, OffsetTop = -82, OffsetBottom = -12 };
        bottom.AddThemeStyleboxOverride("panel", PanelStyle("f0dfb8")); AddChild(bottom);
        var rows = new VBoxContainer(); bottom.AddChild(rows); seed = new Label { HorizontalAlignment = HorizontalAlignment.Center }; rows.AddChild(seed);
        var bar = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }; rows.AddChild(bar); tools = new Button[Tools.Length];
        for (int i = 0; i < Tools.Length; i++)
        {
            int index = i; var b = Button((i + 1) + " " + Tools[i].Label, () => { farm.Tool = Tools[index].Action; Refresh(); });
            b.Icon = farm.Art.Region(Tools[i].Icon); b.ExpandIcon = true; b.AddThemeConstantOverride("icon_max_width", 20); b.CustomMinimumSize = new Vector2(67, 32);
            bar.AddChild(b); tools[i] = b;
        }
        toast = new Label { AnchorLeft = .5f, AnchorRight = .5f, AnchorTop = 1, AnchorBottom = 1, OffsetLeft = -360, OffsetRight = 360, OffsetTop = -134, OffsetBottom = -91,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, Visible = false };
        toast.AddThemeColorOverride("font_color", new Color("fff2cc")); toast.AddThemeColorOverride("font_shadow_color", new Color("2e3f32")); toast.AddThemeConstantOverride("shadow_offset_x", 1); toast.AddThemeConstantOverride("shadow_offset_y", 2); AddChild(toast);
        modal = new Control { AnchorRight = 1, AnchorBottom = 1, Visible = false, MouseFilter = MouseFilterEnum.Stop }; AddChild(modal);
        var dim = new ColorRect { AnchorRight = 1, AnchorBottom = 1, Color = new Color(0.08f, .13f, .1f, .65f) }; modal.AddChild(dim);
        var center = new CenterContainer { AnchorRight = 1, AnchorBottom = 1 }; modal.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(500, 350) }; panel.AddThemeStyleboxOverride("panel", PanelStyle("f3e5c7")); center.AddChild(panel);
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
    public void Refresh()
    {
        var state = farm.Session.Read.World;
        string s = $"{GameCatalog.SeasonNames[state.Season]} · วันที่ {state.SeasonDay} · {(int)state.Minute / 60:00}:{(int)state.Minute % 60:00} · {state.Weather}";
        if (s != lastStatus) { status.Text = s; lastStatus = s; }
        string c = $"● {state.Coins:N0} เหรียญ   แรง"; if (c != lastCoins) { coins.Text = c; lastCoins = c; }
        energy.Value = farm.Session.Read.Player.Energy;
        string chosen = $"{GameCatalog.Crops[farm.Seed].Name} · Tab เปลี่ยนเมล็ด · {Tools.First(t => t.Action == farm.Tool).Label}";
        if (chosen != lastSeed) { seed.Text = chosen; lastSeed = chosen; }
        for (int i = 0; i < tools.Length; i++) tools[i].Modulate = Tools[i].Action == farm.Tool ? new Color("ffd484") : Colors.White;
    }
    void Open(string title)
    {
        foreach (Node child in modalContent.GetChildren()) { modalContent.RemoveChild(child); child.QueueFree(); }
        modalContent.AddChild(new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center });
        modalContent.AddChild(Button("ปิด · Esc", CloseModal)); modal.Visible = true; farm.Farmer.InputBlocked = true;
    }
    public void CloseModal() { modal.Visible = false; farm.Farmer.InputBlocked = false; }
    public void OpenBag()
    {
        Open("กระเป๋าผลผลิต");
        var inventory = farm.Session.Read.Inventory();
        if (inventory.Count == 0) modalContent.AddChild(new Label { Text = "ยังไม่มีผลผลิตในกระเป๋า" });
        foreach (var pair in inventory) modalContent.AddChild(new Label { Text = $"{pair.Name} × {pair.Quantity} · ราคาวันนี้ {pair.UnitPrice} เหรียญ" });
        modalContent.AddChild(Button("ขายทั้งหมด · ต้องยืนใกล้ลังขาย", () => { CloseModal(); farm.Perform("sell"); }));
        modalContent.AddChild(new Label { Text = "ผลผลิตลด 25% เมื่อข้ามคืน ควรขายก่อนเข้านอน" });
    }
    public void OpenShop()
    {
        Open("เมล็ดและอุปกรณ์");
        foreach (var key in farm.CropKeys)
        {
            string cropKey = key; var crop = GameCatalog.Crops[key];
            var row = Button($"{crop.Name} · {crop.Cost} เหรียญ · {crop.Days} วัน" + (crop.Seasons.Contains(farm.Session.Read.World.Season) ? " · ฤดูนี้" : ""), () => { farm.Seed = cropKey; CloseModal(); Refresh(); });
            row.Icon = farm.Art.Region(key); row.ExpandIcon = true; row.AddThemeConstantOverride("icon_max_width", 24); modalContent.AddChild(row);
        }
        modalContent.AddChild(Button("หลังคาเรือนกระจก · 300 เหรียญ · ใกล้ร้านค้า", () => { CloseModal(); farm.Perform("greenhouse"); }));
        modalContent.AddChild(Button("ทุนฉุกเฉิน · เงินต่ำกว่า 6 เหรียญ", () => { CloseModal(); farm.Perform("rescue"); }));
    }
    public void OpenSettings()
    {
        Open("ตั้งค่าภาพและเซฟ");
        for (int i = 0; i < 3; i++) { int q = i; modalContent.AddChild(Button(new[] { "ต่ำ", "กลาง", "สูง" }[i], () => { farm.SaveQuality(q); farm.Notify("ตั้งค่าคุณภาพภาพแล้ว"); })); }
        modalContent.AddChild(Button("วิทยุ YouTube · เปิดในเบราว์เซอร์", OpenMusic));
        modalContent.AddChild(Button("วิธีเล่น", OpenHelp));
        modalContent.AddChild(Button("นำเข้าเซฟเกมเดิม (คัดลอก)", () => {
            var dialog = new ConfirmationDialog { DialogText = "แทนฟาร์ม Godot ด้วยสำเนาเซฟเดิม?\nเซฟเกมเดิมจะไม่ถูกแก้ไข และสำรองฟาร์ม Godot ปัจจุบันไว้ก่อน" };
            AddChild(dialog); dialog.Confirmed += () => { farm.ImportLegacySave(); dialog.QueueFree(); }; dialog.Canceled += () => dialog.QueueFree(); dialog.PopupCentered(); }));
        modalContent.AddChild(Button("บันทึกฟาร์ม", farm.SaveNow));
    }
    public void OpenHelp()
    {
        Open("สวนเพลง · วิธีเล่น");
        modalContent.AddChild(new Label { Text = "WASD / ลูกศร เดิน · 1–7 เครื่องมือ · Tab เมล็ด\nคลิกแปลงใกล้ตัวเพื่อปลูก รดน้ำ เก็บเกี่ยว\nE ขายใกล้ลังด้านขวา · Space นอนหน้าบ้าน\nEsc หยุดเวลา · B กระเป๋า · O ร้าน · F3 ตั้งค่า\nฤดูละ 14 วัน รดน้ำทุกวัน ระวังโรคและพายุ\nฟาร์มฉากนี้เป็นโหมดคนเดียว ยังไม่เปิดห้องออนไลน์\nเซฟทดสอบแยกจากเกม Windows เดิม" });
    }
    void OpenMusic()
    {
        Open("วิทยุ YouTube");
        modalContent.AddChild(new Label { Text = "วางลิงก์ HTTPS ของ YouTube เพื่อฟังในเบราว์เซอร์" });
        var input = new LineEdit { PlaceholderText = "https://www.youtube.com/watch?v=…", MaxLength = 512 }; modalContent.AddChild(input);
        modalContent.AddChild(Button("เปิดเพลง", () => {
            if (!Uri.TryCreate(input.Text.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo != "" || !uri.IsDefaultPort || !new[] { "youtube.com", "www.youtube.com", "music.youtube.com", "m.youtube.com", "youtu.be" }.Contains(uri.Host)) { farm.Notify("ใส่ลิงก์ HTTPS ของ YouTube เท่านั้น"); return; }
            if (OS.ShellOpen(uri.AbsoluteUri) != Error.Ok) farm.Notify("เปิดเบราว์เซอร์ไม่ได้"); else CloseModal();
        }));
    }
    public override void _Input(InputEvent ev)
    {
        if (ModalVisible && ev is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape }) { CloseModal(); GetViewport().SetInputAsHandled(); }
    }
    public override void _Process(double delta) { refreshElapsed += delta; if (dirty && refreshElapsed >= .25) { dirty = false; refreshElapsed = 0; Refresh(); } if (toastTime > 0) { toastTime -= delta; if (toastTime <= 0) toast.Visible = false; } }
}
