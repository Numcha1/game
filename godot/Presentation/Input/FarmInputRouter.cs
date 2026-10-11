using Godot;
namespace SeasonsTogether;
public static class FarmInputRouter
{
    public static void Handle(FarmScene farm, InputEvent ev)
    {
        if (!farm.ReadyToPlay || farm.IsBusy || farm.Hud.ModalVisible) return;
        if (ev is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            Vector2 mouse = farm.GetViewport().GetCanvasTransform().AffineInverse() * click.Position;
            int x = (int)Math.Round(mouse.X / 32), y = (int)Math.Round(mouse.Y / 32);
            if (x >= 8 && x <= 13 && y >= 6 && y <= 9) farm.Perform(farm.Tool, (y - 6) * 6 + x - 8);
        }
        if (ev is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.PhysicalKeycode >= Key.Key1 && key.PhysicalKeycode <= Key.Key7) { farm.Tool = FarmHud.Tools[(int)(key.PhysicalKeycode - Key.Key1)].Action; farm.Hud.Refresh(); }
            switch (key.PhysicalKeycode)
            {
                case Key.Tab: farm.Seed = farm.CropKeys[(Array.IndexOf(farm.CropKeys, farm.Seed) + 1) % 8]; farm.Hud.Refresh(); break;
                case Key.E: farm.Perform("sell"); break;
                case Key.Space: farm.Perform(farm.Session.Read.Player.Sleeping ? "wake" : "sleep"); break;
                case Key.B: case Key.I: farm.Hud.OpenBag(); break;
                case Key.O: farm.Hud.OpenShop(); break;
                case Key.F3: farm.Hud.OpenSettings(); break;
                case Key.Escape: farm.Perform("pause"); break;
                case Key.F1: farm.Hud.OpenHelp(); break;
            }
        }
    }
}
