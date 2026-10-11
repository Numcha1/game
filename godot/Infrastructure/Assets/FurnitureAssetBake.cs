using Godot;
namespace SeasonsTogether;
/// <summary>Offline import preparation of original generated artwork. Never used in gameplay.</summary>
public static class FurnitureAssetBake
{
    public static void Write()
    {
        using var source=Image.LoadFromFile(ProjectSettings.GlobalizePath("res://Assets/Furniture/furniture-source.png"));
        using var target=Image.CreateEmpty(400,192,false,Image.Format.Rgba8);target.Fill(Colors.Transparent);
        Rect2I[] regions={new(48,100,450,330),new(530,45,250,390),new(835,145,270,265),new(1170,135,320,285),new(45,480,335,430),new(408,550,475,355),new(915,490,230,445),new(1200,540,300,370),new(120,150,270,80)};
        Vector2I[] sizes={new(64,48),new(32,48),new(40,40),new(48,40),new(48,72),new(64,56),new(32,72),new(32,48),new(32,14)};
        for(int i=0;i<regions.Length;i++) {
            var cell=regions[i];int left=cell.End.X,top=cell.End.Y,right=cell.Position.X,bottom=cell.Position.Y;
            for(int y=cell.Position.Y;y<cell.End.Y;y++)for(int x=cell.Position.X;x<cell.End.X;x++)if(source.GetPixel(x,y).A>.15f){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
            if(left>right)throw new InvalidDataException("Empty furniture frame "+i);
            using var frame=source.GetRegion(new Rect2I(left,top,right-left+1,bottom-top+1));float ratio=Math.Min(sizes[i].X/(float)frame.GetWidth(),sizes[i].Y/(float)frame.GetHeight());
            int w=Math.Max(1,(int)Math.Round(frame.GetWidth()*ratio)),h=Math.Max(1,(int)Math.Round(frame.GetHeight()*ratio));frame.Resize(w,h,Image.Interpolation.Nearest);
            target.BlitRect(frame,new Rect2I(0,0,w,h),new Vector2I(i%5*80+(80-w)/2,i/5*96+90-h));
        }
        if(target.SavePng(ProjectSettings.GlobalizePath("res://Assets/Furniture/furniture-atlas.png"))!=Error.Ok)throw new IOException("Cannot bake furniture atlas");
    }
}
