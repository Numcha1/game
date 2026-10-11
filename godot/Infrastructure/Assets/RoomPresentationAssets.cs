using Godot;
namespace SeasonsTogether;
/// <summary>Prepared room borders and floor silhouettes shared by scene reloads.</summary>
public static class RoomPresentationAssets
{
    static readonly Dictionary<string,Texture2D> padded=new();
    static readonly Dictionary<string,Texture2D> flat=new();
    public static Texture2D Flat(string key,Texture2D source){if(flat.TryGetValue(key,out var existing))return existing;using var image=source.GetImage();image.Convert(Image.Format.Rgba8);image.Resize(768,512,Image.Interpolation.Nearest);return flat[key]=ImageTexture.CreateFromImage(image);}
    public static Texture2D Padded(string key,Texture2D source)
    {
        if(padded.TryGetValue(key,out var existing))return existing;
        using var image=source.GetImage();image.Convert(Image.Format.Rgba8);image.Resize(768,512,Image.Interpolation.Nearest);
        using var result=Image.CreateEmpty(800,512,false,Image.Format.Rgba8);
        result.BlitRect(image,new Rect2I(0,0,768,512),new Vector2I(16,0));
        result.BlitRect(image,new Rect2I(0,0,16,512),Vector2I.Zero);
        result.BlitRect(image,new Rect2I(752,0,16,512),new Vector2I(784,0));
        return padded[key]=ImageTexture.CreateFromImage(result);
    }
}
