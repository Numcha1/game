using Godot;
namespace SeasonsTogether;

/// <summary>One imported theme and bundled Thai fonts, shared across scene reloads.</summary>
public static class FarmTypography
{
    static Theme? shared;
    public static Theme Shared => shared ??= GD.Load<Theme>("res://Presentation/UI/FarmTheme.tres")
        ?? throw new InvalidDataException("Required farm typography theme missing");
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
        return scales[index] = theme;
    }
}
