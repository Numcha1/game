using Godot;
using SeasonsTogether.Core.Infrastructure.Configuration;
namespace SeasonsTogether;

public partial class FarmScene
{
    DisplaySettings displaySettings = null!;
    public DisplayPreferences Display { get; private set; } = new();
    Task pendingDisplay = Task.CompletedTask;
    Vector2I windowedSize = new(1600, 900);
    bool IsDiagnosticSession => arguments.Any(a => a is "--smoke" or "--showcase" or "--lifecycle" or "--startup-checks" or "--recovery-check" or "--premium-checks" or "--building-checks");
    void InitializeDisplay()
    {
        displaySettings = new DisplaySettings(DataDirectory); Display = displaySettings.Load();
        Hud.SetTextScale(Display.TextScale); Hud.SetUiScale(Display.UiScale); ApplyMotion();
        audio.Volume=Display.EffectsVolume;
        // Test instances share a root window: only the display harness deliberately switches modes.
        if (!IsDiagnosticSession) ApplyWindowMode(Display.Mode);
    }
    public void SetDisplayMode(int mode)
    {
        Display = (Display with { Mode = mode }).Normalize(); ApplyWindowMode(Display.Mode); QueueDisplaySave();
    }
    void ApplyWindowMode(int mode)
    {
        if (DisplayServer.GetName() == "headless") return;
        var window = GetWindow();
        if (window.Mode == Window.ModeEnum.Windowed && !window.Borderless) windowedSize = window.Size;
        if (mode == 1) { window.Borderless = false; window.Mode = Window.ModeEnum.Fullscreen; return; }
        window.Mode = Window.ModeEnum.Windowed; window.Borderless = mode == 2;
        // Preserve the chosen window size. Covering the exact monitor bounds can make Windows
        // promote a borderless window to exclusive fullscreen, defeating the separate mode.
        window.Size = windowedSize;
    }
    public void SetTextScale(int scale)
    {
        Display = (Display with { TextScale = scale }).Normalize(); Hud.SetTextScale(Display.TextScale); QueueDisplaySave();
    }
    public void SetReducedMotion(bool value)
    {
        Display = Display with { ReducedMotion = value }; ApplyMotion(); QueueDisplaySave();
    }
    public void SetEffectsVolume(int value)
    {
        Display=(Display with{EffectsVolume=value}).Normalize();audio.Volume=Display.EffectsVolume;QueueDisplaySave();
    }
    public void SetUiScale(int value)
    {
        Display=(Display with{UiScale=value}).Normalize();Hud.SetUiScale(Display.UiScale);QueueDisplaySave();
    }
    void ApplyMotion()
    {
        weather.ReducedMotion = Display.ReducedMotion; accents.ReducedMotion = Display.ReducedMotion; effects.ReducedMotion=Display.ReducedMotion; renderer.SetReducedMotion(Display.ReducedMotion);
    }
    void QueueDisplaySave() => pendingDisplay = SaveDisplay();
    async Task SaveDisplay()
    {
        try { await displaySettings.SaveAsync(Display); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Notify("บันทึกการตั้งค่าหน้าจอไม่ได้: " + e.Message); }
    }
}
