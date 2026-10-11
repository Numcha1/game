using Godot;
namespace SeasonsTogether;
public static class FarmInputRouter
{
    public static void Handle(FarmScene farm, InputEvent ev)
    {
        if (!farm.ReadyToPlay || farm.IsBusy || farm.Hud.ModalVisible) return;
        if (ev is InputEventMouseMotion motion) Inspect(farm, motion.Position);
        if (ev is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            Vector2 mouse = farm.GetViewport().GetCanvasTransform().AffineInverse() * click.Position;
            int x = (int)Math.Round(mouse.X / 32), y = (int)Math.Round(mouse.Y / 32);
            if (x >= 8 && x <= 13 && y >= 6 && y <= 9) { int plot = (y - 6) * 6 + x - 8; farm.InspectPlot(plot); farm.Hud.RequestPlotAction(farm.Tool, plot); }
        }
        if (ev is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.PhysicalKeycode >= Key.Key1 && key.PhysicalKeycode <= Key.Key7) { farm.Tool = FarmHud.Tools[(int)(key.PhysicalKeycode - Key.Key1)].Action; farm.Hud.Refresh(); }
            switch (key.PhysicalKeycode)
            {
                case Key.Tab: farm.Seed = farm.CropKeys[(Array.IndexOf(farm.CropKeys, farm.Seed) + 1) % 8]; farm.Hud.Refresh(); break;
                case Key.E: farm.Hud.RequestSale(); break;
                case Key.Space: farm.Hud.RequestSleep(); break;
                case Key.B: case Key.I: farm.Hud.OpenBag(); break;
                case Key.O: farm.Hud.OpenShop(); break;
                case Key.F3: farm.Hud.OpenSettings(); break;
                case Key.Escape: farm.Hud.OpenPause(); break;
                case Key.F1: farm.Hud.OpenHelp(); break;
            }
        }
    }
    static void Inspect(FarmScene farm, Vector2 viewportPosition)
    {
        Vector2 mouse = farm.GetViewport().GetCanvasTransform().AffineInverse() * viewportPosition;
        int x = (int)Math.Round(mouse.X / 32), y = (int)Math.Round(mouse.Y / 32);
        if (x >= 8 && x <= 13 && y >= 6 && y <= 9) farm.InspectPlot((y - 6) * 6 + x - 8);
        else farm.InspectPlot(-1);
    }
}
