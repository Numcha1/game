using Godot;
using SeasonsTogether.Core.Application;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether;

public partial class NpcActor : Node2D
{
    GameSession session=null!;string id="",region="";
    AnimatedSprite2D sprite=null!;
    bool positioned;
    Vector2 previousWorld;
    public void Initialize(GameSession game,string npcId,string room)
    {
        session=game;id=npcId;region=room;Name="Npc-"+id;
        TextureFilter=TextureFilterEnum.Nearest;
        AddChild(new Sprite2D{Texture=AssetCatalog.Shared.Shadow,Scale=new Vector2(.65f,.45f),Modulate=new Color(1,1,1,.55f)});
        float scale=44f/(NpcAssets.Characters.GetHeight()/4f*.92f);
        sprite=new AnimatedSprite2D{SpriteFrames=NpcAssets.Frames(id),Scale=Vector2.One*scale,Offset=new Vector2(0,-NpcAssets.Characters.GetHeight()/8f),Animation="down"};AddChild(sprite);
        var label=new Label{Text=NpcRegistry.Find(id)!.Name,Theme=FarmTypography.Shared,Position=new Vector2(-40,-55),Size=new Vector2(80,20),HorizontalAlignment=HorizontalAlignment.Center};
        label.AddThemeFontSizeOverride("font_size",12);label.AddThemeColorOverride("font_color",new Color("fff4d5"));label.AddThemeColorOverride("font_shadow_color",new Color("332f26"));label.AddThemeConstantOverride("shadow_offset_x",1);label.AddThemeConstantOverride("shadow_offset_y",1);AddChild(label);
    }
    public override void _Process(double delta)
    {
        if(session==null || !session.TryNpc(id,out var npc))return;
        Visible=npc.Region==region;if(!Visible){positioned=false;return;}
        var next=new Vector2((float)npc.X*32,(float)npc.Y*32);var movement=next-previousWorld;
        if(positioned && movement.LengthSquared()>.005f) {
            string direction=Math.Abs(movement.X)>Math.Abs(movement.Y)?movement.X>0?"right":"left":movement.Y>0?"down":"up";sprite.Play(direction);
        } else {sprite.Stop();sprite.Frame=0;}
        Position=next.Round();previousWorld=next;positioned=true;
    }
}
