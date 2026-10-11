namespace SeasonsTogether;

/// <summary>Logical viewport pixels; accessibility scaling is applied by the HUD.</summary>
public static class PresentationScale
{
    public const int SafeMargin=16, Gap=8, Padding=8;
    public const int HudHeight=48, ButtonHeight=28, ButtonWidth=96;
    public const int HudFont=12, PromptFont=12;
    public const float IndoorZoom=1.25f, IndoorCharacter=1.05f;
}
