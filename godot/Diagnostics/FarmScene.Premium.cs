using Godot;
using System.Text.Json;
namespace SeasonsTogether;

public partial class FarmScene
{
    async Task RunPremiumChecks()
    {
        var checks = new List<string>(); var surfaces = new List<object>();
        void Check(bool pass, string label) { if (!pass) throw new InvalidOperationException(label); checks.Add(label); }
        try {
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(Hud.Theme.DefaultFont is FontFile && Hud.Theme.DefaultFont.ResourcePath.Contains("Sarabun-Regular.ttf"), "bundled Sarabun body font without system font dependency");
            Check(Hud.Theme.GetFont("font", "Heading").ResourcePath.Contains("Mitr-Medium.ttf"), "bundled Mitr heading hierarchy");
            const string thai = "น้ำ ปุ๋ย เก็บเกี่ยว ฤดูใบไม้ผลิ ผู้เล่น กิ่กี้กึ๊กื๋";
            string supported = Hud.Theme.DefaultFont.GetSupportedChars();
            Check(thai.Where(c => !char.IsWhiteSpace(c)).All(c => supported.Contains(c)), "Thai consonants vowels and tone marks present in bundled font");
            Check(Hud.Theme.GetStylebox("focus", "Button") != null && Hud.Theme.GetStylebox("disabled", "Button") != null, "reusable focus and disabled button theme states");
            string initialSeed = Seed;
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Tab, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Tab, Pressed = false });
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(Seed != initialSeed, "original Tab seed shortcut preserved outside modal");
            Hud.OpenShop();
            var firstFocus = GetViewport().GuiGetFocusOwner();
            Check(firstFocus is Button, "modal opens with keyboard focus");
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.Tab, PhysicalKeycode = Key.Tab, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.Tab, PhysicalKeycode = Key.Tab, Pressed = false });
            await NextFrame(SceneTree.SignalName.ProcessFrame);
            Check(GetViewport().GuiGetFocusOwner() is Button && GetViewport().GuiGetFocusOwner() != firstFocus, "Tab navigates shop controls");
            Hud.CloseModal();
            Check(GetViewport().GuiGetFocusOwner() == null && !Farmer.InputBlocked, "closing modal releases focus and gameplay input");
            SetReducedMotion(true);
            Check(weather.ReducedMotion && Display.ReducedMotion, "reduced motion disables ambient rain and foliage motion");
            SetReducedMotion(false);
            if (DisplayServer.GetName() != "headless") {
                foreach (int mode in new[] { 1, 2, 0 }) {
                    SetDisplayMode(mode);
                    for (int n = 0; n < 8; n++) await NextFrame(SceneTree.SignalName.ProcessFrame);
                    Check(mode == 1 ? GetWindow().Mode == Window.ModeEnum.Fullscreen : GetWindow().Mode == Window.ModeEnum.Windowed && GetWindow().Borderless == (mode == 2), "actual native display mode " + mode + " reported " + GetWindow().Mode + " borderless " + GetWindow().Borderless);
                }
                foreach (var size in new[] { new Vector2I(1280,720), new Vector2I(1920,1080), new Vector2I(2560,1440), new Vector2I(3840,2160) }) {
                    GetWindow().Size = size;
                    for (int n = 0; n < 10; n++) await NextFrame(SceneTree.SignalName.ProcessFrame);
                    Check(GetWindow().Size == size, "requested native window resolution " + size);
                    Check(GetViewport().GetVisibleRect().Size == new Vector2(800,450), "logical viewport preserved " + size);
                    for (int scale = 0; scale < 3; scale++) {
                        SetTextScale(scale); Hud.OpenHelp();
                        for (int n = 0; n < 3; n++) await NextFrame(SceneTree.SignalName.ProcessFrame);
                        var panels = Hud.FindChildren("*", "PanelContainer", true, false).OfType<PanelContainer>().Where(p => p.IsVisibleInTree());
                        Check(panels.All(p => new Rect2(Vector2.Zero, Hud.Size).Encloses(p.GetGlobalRect())), "HUD and modal bounds at " + size + " text scale " + scale);
                        await NextDraw();
                        if (scale == 0) {
                            using var capture = GetViewport().GetTexture().GetImage();
                            int pixelScale = Math.Max(1, Math.Min(size.X / 800, size.Y / 450));
                            Check(capture.GetSize() == new Vector2I(800 * pixelScale, 450 * pixelScale), "integer-scaled renderer surface with letterboxing " + size);
                            capture.SavePng(Path.Combine(ProjectSettings.GlobalizePath("res://reports"), "premium-" + size.X + "x" + size.Y + ".png"));
                            surfaces.Add(new { requestedWidth=size.X,requestedHeight=size.Y,renderWidth=capture.GetWidth(),renderHeight=capture.GetHeight(),pixelScale,letterboxX=(size.X-capture.GetWidth())/2,letterboxY=(size.Y-capture.GetHeight())/2 });
                        }
                        Hud.CloseModal();
                    }
                    var canvas = GetViewport().GetCanvasTransform();
                    Check(Enumerable.Range(0,24).All(i => { var t=Core.Domain.GameCatalog.PlotTile(i); var p=new Vector2(t.X*32,t.Y*32); return (canvas.AffineInverse()*(canvas*p)).DistanceTo(p)<.001f; }), "all plot picks preserve camera inverse " + size);
                }
                SetTextScale(0); GetWindow().Size = new Vector2I(1600,900);
                for (int n = 0; n < 10; n++) await NextFrame(SceneTree.SignalName.ProcessFrame);
                using var pattern = Image.CreateEmpty(8,8,false,Image.Format.Rgba8);
                for (int y=0;y<8;y++) for(int x=0;x<8;x++) pattern.SetPixel(x,y,(x+y)%2==0?Colors.Red:Colors.Blue);
                using var texture = ImageTexture.CreateFromImage(pattern);
                var marker = new Sprite2D { Texture=texture,Centered=false,Position=GetViewport().GetCanvasTransform().AffineInverse()*new Vector2(400,225),ZIndex=1000,LightMask=0 };
                AddChild(marker); await NextDraw();
                using (var capture = GetViewport().GetTexture().GetImage()) {
                    bool sharp = true;
                    for(int y=0;y<8;y++) for(int x=0;x<8;x++) {
                        var color=capture.GetPixel(800+x*2,450+y*2);
                        for(int dy=0;dy<2;dy++) for(int dx=0;dx<2;dx++) sharp &= capture.GetPixel(800+x*2+dx,450+y*2+dy)==color;
                    }
                    Check(sharp && capture.GetPixel(800,450)!=capture.GetPixel(802,450), "nearest world pixels form uniform 2x2 blocks without interpolation");
                }
                RemoveChild(marker); marker.QueueFree();
            }
            await pendingDisplay;
            Check(displaySettings.Load() == Display, "display settings flushed without farm schema changes");
            await Session.SaveAsync(); finalCheckpoint = true;
            await System.IO.File.WriteAllTextAsync(Path.Combine(ProjectSettings.GlobalizePath("res://reports"),"premium-" + DisplayServer.GetName() + ".json"), JsonSerializer.Serialize(new { passed=checks.Count,checks,surfaces,scope="Real display/font/layout/input checks; screenshots require visual review; no complete art or lifetime performance certification." },new JsonSerializerOptions { WriteIndented=true }));
            GD.Print("PREMIUM PASS " + checks.Count); GetTree().Quit();
        }
        catch (Exception e) { GD.PushError("PREMIUM FAIL " + e); GetTree().Quit(1); }
    }
}
