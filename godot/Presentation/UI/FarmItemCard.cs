using Godot;
namespace SeasonsTogether;

/// <summary>Reusable focusable item presentation; the caller owns the command.</summary>
public partial class FarmItemCard : Button
{
    bool configured;
    public void MakeInventorySlot(int quantity)
    {
        CustomMinimumSize=new Vector2(72,62);
        GetNode<Control>("Margin/Row/Text").Visible=false;
        GetNode<TextureRect>("Margin/Row/Icon").CustomMinimumSize=new Vector2(32,40);
        var count=new Label {Name="SlotCount",Text=quantity<1000000?quantity.ToString("N0"): (quantity/1000000d).ToString("0.#")+"m",
            AnchorRight=1,AnchorTop=1,AnchorBottom=1,OffsetTop=-20,OffsetBottom=-3,OffsetRight=-6,
            HorizontalAlignment=HorizontalAlignment.Right,MouseFilter=MouseFilterEnum.Ignore};
        count.AddThemeFontSizeOverride("font_size",11);AddChild(count);
    }
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
