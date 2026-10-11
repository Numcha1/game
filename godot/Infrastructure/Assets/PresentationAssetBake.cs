using Godot;
namespace SeasonsTogether;
/// <summary>Explicit development-time preparation, never called from the game loop.</summary>
public static class PresentationAssetBake
{
    public static void WriteNpcAtlas()
    {
        using var source=Image.LoadFromFile(ProjectSettings.GlobalizePath("res://Assets/Village/npc-atlas.png"));
        using var target=Image.CreateEmpty(320,224,false,Image.Format.Rgba8);target.Fill(Colors.Transparent);
        for(int row=0;row<4;row++)for(int col=0;col<8;col++) {
            int x0=col*source.GetWidth()/8,x1=(col+1)*source.GetWidth()/8,y0=row*source.GetHeight()/4,y1=(row+1)*source.GetHeight()/4;
            int left=x1,top=y1,right=x0,bottom=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(source.GetPixel(x,y).A>.15f){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
            if(left>right)throw new InvalidDataException("Empty NPC source frame");
            int width=right-left+1,height=bottom-top+1;
            float ratio=Math.Min(36f/width,52f/height);
            using var frame=source.GetRegion(new Rect2I(left,top,width,height));
            int w=Math.Max(1,(int)Math.Round(width*ratio)),h=Math.Max(1,(int)Math.Round(height*ratio));frame.Resize(w,h,Image.Interpolation.Nearest);
            target.BlitRect(frame,new Rect2I(0,0,w,h),new Vector2I(col*40+(40-w)/2,row*56+54-h));
        }
        var error=target.SavePng(ProjectSettings.GlobalizePath("res://Assets/Village/npc-logical.png"));
        if(error!=Error.Ok)throw new IOException("Cannot prepare NPC atlas: "+error);
    }
}
