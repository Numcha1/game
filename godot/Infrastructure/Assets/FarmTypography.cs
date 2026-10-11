using Godot;
namespace SeasonsTogether;

/// <summary>One imported theme and bundled Thai fonts, shared across scene reloads.</summary>
public static class FarmTypography
{
    static Theme? shared;
    public static Theme Shared {
        get {
            if(shared!=null)return shared;
            var theme=GD.Load<Theme>("res://Presentation/UI/FarmTheme.tres") ?? throw new InvalidDataException("Required farm typography theme missing");
            Validate(theme);return shared=theme;
        }
    }
    public static void Validate(Theme theme) {
        if(theme.DefaultFont is not FontFile || !theme.DefaultFont.ResourcePath.Contains("Sarabun-Regular.ttf") ||
            theme.GetFont("font","Heading") is not FontFile || !theme.GetFont("font","Heading").ResourcePath.Contains("Mitr-Medium.ttf") ||
            theme.GetFont("font","Button") is not FontFile || !theme.GetFont("font","Button").ResourcePath.Contains("Sarabun-SemiBold.ttf"))
            throw new InvalidDataException("Required bundled Thai typography missing");
        string supported=theme.DefaultFont.GetSupportedChars();
        if(!"กิ่กี้กึ๊กื๋ปุ๋ยน้ำ".All(supported.Contains))throw new InvalidDataException("Bundled font lacks Thai coverage");
    }
    static readonly Theme?[] scales = new Theme?[3];
    public static Theme AtScale(int index)
    {
        index = Math.Clamp(index, 0, 2);
        if (index == 0) return Shared;
        if (scales[index] != null) return scales[index]!;
        var theme = (Theme)Shared.Duplicate();
        theme.DefaultFontSize = 14 + index * 2;
        theme.SetFontSize("font_size", "Heading", 20 + index * 2);
        theme.SetFontSize("font_size", "Compact", 13 + index);
        theme.SetFontSize("font_size", "ItemHeading", 16 + index*2);
        return scales[index] = theme;
    }
}
