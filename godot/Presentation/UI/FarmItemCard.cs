using Godot;
namespace SeasonsTogether;

/// <summary>Reusable focusable item presentation; the caller owns the command.</summary>
public partial class FarmItemCard : Button
{
    bool configured;
    public void Configure(string title,string description,Texture2D icon,Action selected,bool highlighted=false)
    {
        if(configured)throw new InvalidOperationException("Item card already configured");configured=true;
        GetNode<Label>("Margin/Row/Text/Name").Text=title;
        GetNode<Label>("Margin/Row/Text/Description").Text=description;
        GetNode<TextureRect>("Margin/Row/Icon").Texture=icon;
        TooltipText=title+"\n"+description;Pressed+=selected;
        if(highlighted)AddThemeStyleboxOverride("normal",GetThemeStylebox("pressed"));
    }
}
